using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCreditDueDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "current_due_date",
                schema: "pos",
                table: "credit_entries",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "credit_due_date_changes",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    new_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_due_date_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_credit_due_date_changes_credit_entries",
                        column: x => x.credit_entry_id,
                        principalSchema: "pos",
                        principalTable: "credit_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_credit_due_date_changes_customers",
                        column: x => x.customer_id,
                        principalSchema: "pos",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credit_entries_org_current_due_date",
                schema: "pos",
                table: "credit_entries",
                columns: new[] { "organization_id", "current_due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_due_date_changes_credit_entry_id",
                schema: "pos",
                table: "credit_due_date_changes",
                column: "credit_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_due_date_changes_customer_id",
                schema: "pos",
                table: "credit_due_date_changes",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_due_date_changes_org_changed",
                schema: "pos",
                table: "credit_due_date_changes",
                columns: new[] { "organization_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_credit_due_date_changes_org_credit_changed",
                schema: "pos",
                table: "credit_due_date_changes",
                columns: new[] { "organization_id", "credit_entry_id", "changed_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_due_date_changes",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ix_credit_entries_org_current_due_date",
                schema: "pos",
                table: "credit_entries");

            migrationBuilder.DropColumn(
                name: "current_due_date",
                schema: "pos",
                table: "credit_entries");
        }
    }
}
