using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBnplCustomerFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bnpl");

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "bnpl",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    mobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    normalized_mobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    linked_personal_public_user_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    linked_commerce_customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                    table.CheckConstraint("ck_bnpl_customers_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_customers_org_display_name",
                schema: "bnpl",
                table: "customers",
                columns: new[] { "organization_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_customers_org_normalized_email",
                schema: "bnpl",
                table: "customers",
                columns: new[] { "organization_id", "normalized_email" });

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_customers_org_normalized_mobile",
                schema: "bnpl",
                table: "customers",
                columns: new[] { "organization_id", "normalized_mobile" });

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_customers_organization_id",
                schema: "bnpl",
                table: "customers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_bnpl_customers_org_linked_commerce",
                schema: "bnpl",
                table: "customers",
                columns: new[] { "organization_id", "linked_commerce_customer_id" },
                unique: true,
                filter: "linked_commerce_customer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_bnpl_customers_org_linked_personal",
                schema: "bnpl",
                table: "customers",
                columns: new[] { "organization_id", "linked_personal_public_user_id" },
                unique: true,
                filter: "linked_personal_public_user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers",
                schema: "bnpl");
        }
    }
}
