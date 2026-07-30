using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCreditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_entries",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remarks = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reversed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_entries", x => x.id);
                    table.CheckConstraint("ck_credit_entries_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_credit_entries_status", "status IN ('Active', 'Reversed')");
                    table.ForeignKey(
                        name: "fk_credit_entries_customers",
                        column: x => x.customer_id,
                        principalSchema: "pos",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credit_entries_customer_id",
                schema: "pos",
                table: "credit_entries",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_entries_org_customer_created",
                schema: "pos",
                table: "credit_entries",
                columns: new[] { "organization_id", "customer_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_credit_entries_org_customer_status",
                schema: "pos",
                table: "credit_entries",
                columns: new[] { "organization_id", "customer_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_entries",
                schema: "pos");
        }
    }
}
