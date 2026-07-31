using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosRegisters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "register_id",
                schema: "pos",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "refund_register_id",
                schema: "pos",
                table: "sale_returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_register_id",
                schema: "pos",
                table: "sale_returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "register_id",
                schema: "pos",
                table: "cashier_shifts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "register_code_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    next_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_register_code_sequences", x => x.organization_id);
                    table.CheckConstraint("ck_register_code_sequences_next_value_positive", "next_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "registers",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    register_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registers", x => x.id);
                    table.CheckConstraint("ck_registers_code_format", "register_code ~ '^REG-[0-9]{6}$'");
                    table.CheckConstraint("ck_registers_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_sales_register_id",
                schema: "pos",
                table: "sales",
                column: "register_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_refund_register_id",
                schema: "pos",
                table: "sale_returns",
                column: "refund_register_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_source_register_id",
                schema: "pos",
                table: "sale_returns",
                column: "source_register_id");

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shifts_register_id",
                schema: "pos",
                table: "cashier_shifts",
                column: "register_id");

            migrationBuilder.CreateIndex(
                name: "ux_cashier_shifts_org_register_open",
                schema: "pos",
                table: "cashier_shifts",
                columns: new[] { "organization_id", "register_id" },
                unique: true,
                filter: "status = 'Open' AND register_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_registers_org_status",
                schema: "pos",
                table: "registers",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_registers_organization_id",
                schema: "pos",
                table: "registers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_registers_org_normalized_name",
                schema: "pos",
                table: "registers",
                columns: new[] { "organization_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_registers_org_register_code",
                schema: "pos",
                table: "registers",
                columns: new[] { "organization_id", "register_code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_cashier_shifts_registers",
                schema: "pos",
                table: "cashier_shifts",
                column: "register_id",
                principalSchema: "pos",
                principalTable: "registers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_returns_refund_registers",
                schema: "pos",
                table: "sale_returns",
                column: "refund_register_id",
                principalSchema: "pos",
                principalTable: "registers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_returns_source_registers",
                schema: "pos",
                table: "sale_returns",
                column: "source_register_id",
                principalSchema: "pos",
                principalTable: "registers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_registers",
                schema: "pos",
                table: "sales",
                column: "register_id",
                principalSchema: "pos",
                principalTable: "registers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cashier_shifts_registers",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_returns_refund_registers",
                schema: "pos",
                table: "sale_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_returns_source_registers",
                schema: "pos",
                table: "sale_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_registers",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropTable(
                name: "register_code_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "registers",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ix_sales_register_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_returns_refund_register_id",
                schema: "pos",
                table: "sale_returns");

            migrationBuilder.DropIndex(
                name: "ix_sale_returns_source_register_id",
                schema: "pos",
                table: "sale_returns");

            migrationBuilder.DropIndex(
                name: "ix_cashier_shifts_register_id",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropIndex(
                name: "ux_cashier_shifts_org_register_open",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropColumn(
                name: "register_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "refund_register_id",
                schema: "pos",
                table: "sale_returns");

            migrationBuilder.DropColumn(
                name: "source_register_id",
                schema: "pos",
                table: "sale_returns");

            migrationBuilder.DropColumn(
                name: "register_id",
                schema: "pos",
                table: "cashier_shifts");
        }
    }
}
