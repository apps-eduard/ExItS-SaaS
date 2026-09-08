using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Data fix: ingredient-capable products must have a tracked inventory account.
/// Enables tracking on existing accounts and creates missing tracked shells at 0 on-hand.
/// Does not delete stock, movements, or history.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260908120000_EnableTrackingForIngredientProducts")]
public partial class EnableTrackingForIngredientProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE pos.inventory_accounts AS ia
            SET is_tracked = TRUE,
                updated_at_utc = NOW() AT TIME ZONE 'utc'
            FROM pos.products AS p
            WHERE ia.organization_id = p.organization_id
              AND ia.product_id = p.id
              AND p.can_be_used_as_ingredient = TRUE
              AND ia.is_tracked = FALSE;

            INSERT INTO pos.inventory_accounts (
                id,
                organization_id,
                product_id,
                is_tracked,
                reorder_level,
                reorder_quantity,
                on_hand_quantity,
                reserved_quantity,
                created_at_utc,
                updated_at_utc)
            SELECT
                gen_random_uuid(),
                p.organization_id,
                p.id,
                TRUE,
                NULL,
                NULL,
                0,
                0,
                NOW() AT TIME ZONE 'utc',
                NOW() AT TIME ZONE 'utc'
            FROM pos.products AS p
            WHERE p.can_be_used_as_ingredient = TRUE
              AND NOT EXISTS (
                  SELECT 1
                  FROM pos.inventory_accounts AS ia
                  WHERE ia.organization_id = p.organization_id
                    AND ia.product_id = p.id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversible data fix — do not disable tracking or delete accounts.
    }
}
