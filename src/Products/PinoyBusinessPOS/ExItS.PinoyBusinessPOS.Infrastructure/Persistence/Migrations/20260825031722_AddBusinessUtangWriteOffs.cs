using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessUtangWriteOffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "write_offs",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reversed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    reversed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_write_offs", x => x.id);
                    table.CheckConstraint("ck_write_offs_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_write_offs_reversal_consistency", "(status = 'Active' AND reversed_at_utc IS NULL AND reversal_reason IS NULL AND reversed_by IS NULL) OR (status = 'Reversed' AND reversed_at_utc IS NOT NULL AND reversal_reason IS NOT NULL AND reversed_by IS NOT NULL)");
                    table.CheckConstraint("ck_write_offs_status", "status IN ('Active', 'Reversed')");
                    table.ForeignKey(
                        name: "fk_write_offs_customers",
                        column: x => x.customer_id,
                        principalSchema: "pos",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_write_offs_customer_id",
                schema: "pos",
                table: "write_offs",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_write_offs_org_customer_recorded",
                schema: "pos",
                table: "write_offs",
                columns: new[] { "organization_id", "customer_id", "recorded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_write_offs_org_customer_status",
                schema: "pos",
                table: "write_offs",
                columns: new[] { "organization_id", "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_write_offs_organization_id",
                schema: "pos",
                table: "write_offs",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "write_offs",
                schema: "pos");
        }
    }
}
