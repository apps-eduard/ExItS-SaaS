using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <summary>
/// One active product-access row per user and product.
/// Aborts when existing rows already violate that rule. Does not delete or merge them.
///
/// Read-only preflight (do not mutate production):
/// SELECT COUNT(*) AS conflict_count
/// FROM (
///   SELECT user_id, product_code
///   FROM platform.product_access_assignments
///   WHERE status = 'Active'
///   GROUP BY user_id, product_code
///   HAVING COUNT(DISTINCT organization_id) > 1
/// ) conflicts;
/// A nonzero conflict_count means this migration will abort. End one active affiliation per pair, then retry.
/// </summary>
[DbContext(typeof(PlatformDbContext))]
[Migration("20261005130000_OneActiveProductAffiliationPerUser")]
public sealed class OneActiveProductAffiliationPerUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
              conflict_count integer;
            BEGIN
              SELECT COUNT(*) INTO conflict_count
              FROM (
                SELECT user_id, product_code
                FROM platform.product_access_assignments
                WHERE status = 'Active'
                GROUP BY user_id, product_code
                HAVING COUNT(DISTINCT organization_id) > 1
              ) conflicts;

              IF conflict_count > 0 THEN
                RAISE EXCEPTION
                  'Cannot enforce one active organization affiliation per user and product. % user/product pairs already have more than one active organization. Do not delete memberships. End one active affiliation per pair, then retry this migration.',
                  conflict_count;
              END IF;
            END $$;
            """);

        migrationBuilder.CreateIndex(
            name: "ux_product_access_assignments_user_product_active",
            schema: "platform",
            table: "product_access_assignments",
            columns: ["user_id", "product_code"],
            unique: true,
            filter: "status = 'Active'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_product_access_assignments_user_product_active",
            schema: "platform",
            table: "product_access_assignments");
    }
}
