using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PosDbContext))]
    [Migration("20260818223000_AddSaleBranchId")]
    public partial class AddSaleBranchId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                schema: "pos",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_org_branch",
                schema: "pos",
                table: "sales",
                columns: new[] { "organization_id", "branch_id" },
                filter: "branch_id IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sales_org_branch",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "branch_id",
                schema: "pos",
                table: "sales");
        }
    }
}
