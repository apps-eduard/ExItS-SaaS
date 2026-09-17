using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseBranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                schema: "pos",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_branch_id",
                schema: "pos",
                table: "expenses",
                columns: new[] { "organization_id", "branch_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_expenses_org_branch_id",
                schema: "pos",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "branch_id",
                schema: "pos",
                table: "expenses");
        }
    }
}
