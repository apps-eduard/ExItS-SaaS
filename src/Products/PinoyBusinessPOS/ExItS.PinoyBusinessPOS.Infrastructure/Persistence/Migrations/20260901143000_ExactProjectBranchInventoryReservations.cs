using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// MB2-02B-H3: exact branch reservation projection — sets every branch balance
/// reserved_quantity to active aggregate quantity or zero (clears stale reservations).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260901143000_ExactProjectBranchInventoryReservations")]
public partial class ExactProjectBranchInventoryReservations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $cutover$
            BEGIN
              IF EXISTS (
                SELECT 1
                FROM pos.sales s
                WHERE s.stock_reservation_state = 'Reserved'
                  AND s.branch_id IS NULL)
              THEN
                RAISE EXCEPTION
                  'pos.inventory.branch_reservation_cutover.unresolved_sale_branch: Active Reserved sales require durable BranchId; Main is not assumed.';
              END IF;

              IF EXISTS (
                SELECT 1
                FROM pos.customer_orders o
                WHERE o.stock_reservation_state = 'Reserved'
                  AND o.fulfillment_branch_id = '00000000-0000-0000-0000-000000000000'::uuid)
              THEN
                RAISE EXCEPTION
                  'pos.inventory.branch_reservation_cutover.unresolved_order_branch: Active Reserved customer orders require durable FulfillmentBranchId; Main is not assumed.';
              END IF;
            END
            $cutover$;

            CREATE TEMP TABLE tmp_branch_reservation_exact ON COMMIT DROP AS
            SELECT
              src.organization_id,
              src.branch_id,
              src.product_id,
              SUM(src.quantity) AS reserved_quantity
            FROM (
              SELECT
                s.organization_id,
                s.branch_id,
                sl.product_id,
                sl.quantity
              FROM pos.sales s
              INNER JOIN pos.sale_lines sl
                ON sl.sale_id = s.id
               AND sl.organization_id = s.organization_id
              INNER JOIN pos.inventory_accounts ia
                ON ia.organization_id = s.organization_id
               AND ia.product_id = sl.product_id
               AND ia.is_tracked = TRUE
              WHERE s.stock_reservation_state = 'Reserved'
                AND s.branch_id IS NOT NULL

              UNION ALL

              SELECT
                o.seller_organization_id AS organization_id,
                o.fulfillment_branch_id AS branch_id,
                ol.product_id,
                ol.quantity
              FROM pos.customer_orders o
              INNER JOIN pos.customer_order_lines ol
                ON ol.order_id = o.id
               AND ol.seller_organization_id = o.seller_organization_id
              INNER JOIN pos.inventory_accounts ia
                ON ia.organization_id = o.seller_organization_id
               AND ia.product_id = ol.product_id
               AND ia.is_tracked = TRUE
              WHERE o.stock_reservation_state = 'Reserved'
                AND o.fulfillment_branch_id <> '00000000-0000-0000-0000-000000000000'::uuid
            ) src
            GROUP BY src.organization_id, src.branch_id, src.product_id;

            DO $cutover$
            BEGIN
              IF EXISTS (
                SELECT 1
                FROM tmp_branch_reservation_exact t
                LEFT JOIN pos.inventory_branch_balances b
                  ON b.organization_id = t.organization_id
                 AND b.branch_id = t.branch_id
                 AND b.product_id = t.product_id
                WHERE b.product_id IS NULL)
              THEN
                RAISE EXCEPTION
                  'pos.inventory.branch_reservation_cutover.missing_balance: Active reservation targets a branch/product without InventoryBranchBalance; OnHand will not be invented.';
              END IF;

              IF EXISTS (
                SELECT 1
                FROM tmp_branch_reservation_exact t
                INNER JOIN pos.inventory_branch_balances b
                  ON b.organization_id = t.organization_id
                 AND b.branch_id = t.branch_id
                 AND b.product_id = t.product_id
                WHERE t.reserved_quantity > b.on_hand_quantity)
              THEN
                RAISE EXCEPTION
                  'pos.inventory.branch_reservation_cutover.over_reserved: Active reservations exceed branch OnHand; remediating data is required before cutover.';
              END IF;

              IF EXISTS (
                SELECT 1
                FROM pos.inventory_accounts ia
                LEFT JOIN (
                  SELECT organization_id, product_id, SUM(reserved_quantity) AS doc_reserved
                  FROM tmp_branch_reservation_exact
                  GROUP BY organization_id, product_id
                ) doc
                  ON doc.organization_id = ia.organization_id
                 AND doc.product_id = ia.product_id
                WHERE ia.is_tracked = TRUE
                  AND COALESCE(doc.doc_reserved, 0) <> ia.reserved_quantity)
              THEN
                RAISE EXCEPTION
                  'pos.inventory.branch_reservation_cutover.org_mismatch: Organization ReservedQuantity does not equal sum of branch-attributable active reservations.';
              END IF;
            END
            $cutover$;

            UPDATE pos.inventory_branch_balances b
            SET
              reserved_quantity = COALESCE((
                SELECT t.reserved_quantity
                FROM tmp_branch_reservation_exact t
                WHERE t.organization_id = b.organization_id
                  AND t.branch_id = b.branch_id
                  AND t.product_id = b.product_id
              ), 0),
              updated_at_utc = (NOW() AT TIME ZONE 'utc')
            WHERE b.reserved_quantity IS DISTINCT FROM COALESCE((
              SELECT t.reserved_quantity
              FROM tmp_branch_reservation_exact t
              WHERE t.organization_id = b.organization_id
                AND t.branch_id = b.branch_id
                AND t.product_id = b.product_id
            ), 0);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // H3 corrects invalid stale branch reservation projection.
        // No deterministic rollback to pre-H3 stale reserved_quantity values.
    }
}
