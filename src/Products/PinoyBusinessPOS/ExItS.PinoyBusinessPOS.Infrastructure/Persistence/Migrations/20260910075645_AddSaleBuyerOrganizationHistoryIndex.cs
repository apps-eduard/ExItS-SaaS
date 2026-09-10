using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleBuyerOrganizationHistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_sales_buyer_organization_party_recorded",
                schema: "pos",
                table: "sales",
                columns: new[] { "buyer_organization_id", "buyer_party_kind", "recorded_at_utc" },
                filter: "buyer_organization_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sales_buyer_organization_party_recorded",
                schema: "pos",
                table: "sales");
        }
    }
}
