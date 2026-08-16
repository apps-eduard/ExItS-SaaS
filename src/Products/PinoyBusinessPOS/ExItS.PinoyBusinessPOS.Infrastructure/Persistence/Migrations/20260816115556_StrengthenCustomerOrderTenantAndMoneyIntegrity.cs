using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenCustomerOrderTenantAndMoneyIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_customer_orders_customer_buyer_org_created_at",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "customer_buyer_organization_id", "created_at_utc" },
                descending: new[] { false, true },
                filter: "customer_buyer_organization_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_orders_customer_user_created_at",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "customer_platform_user_id", "created_at_utc" },
                descending: new[] { false, true },
                filter: "customer_platform_user_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_orders_delivery_branch_lat_long_pair",
                schema: "pos",
                table: "customer_orders",
                sql: "(delivery_branch_latitude_snapshot IS NULL AND delivery_branch_longitude_snapshot IS NULL) OR (delivery_branch_latitude_snapshot IS NOT NULL AND delivery_branch_longitude_snapshot IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_orders_delivery_destination_lat_long_pair",
                schema: "pos",
                table: "customer_orders",
                sql: "(delivery_destination_latitude IS NULL AND delivery_destination_longitude IS NULL) OR (delivery_destination_latitude IS NOT NULL AND delivery_destination_longitude IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_orders_money_identity",
                schema: "pos",
                table: "customer_orders",
                sql: "total = merchandise_subtotal + delivery_fee");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_orders_party_xor",
                schema: "pos",
                table: "customer_orders",
                sql: "(customer_party_type = 'Personal' AND customer_platform_user_id IS NOT NULL AND customer_buyer_organization_id IS NULL) OR (customer_party_type = 'Organization' AND customer_buyer_organization_id IS NOT NULL AND customer_platform_user_id IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customer_orders_customer_buyer_org_created_at",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropIndex(
                name: "ix_customer_orders_customer_user_created_at",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_orders_delivery_branch_lat_long_pair",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_orders_delivery_destination_lat_long_pair",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_orders_money_identity",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_orders_party_xor",
                schema: "pos",
                table: "customer_orders");
        }
    }
}
