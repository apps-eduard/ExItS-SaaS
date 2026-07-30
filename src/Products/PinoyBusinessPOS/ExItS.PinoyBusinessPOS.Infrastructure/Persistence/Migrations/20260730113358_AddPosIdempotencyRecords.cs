using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosIdempotencyRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    operation_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outcome_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    outcome_body_json = table.Column<string>(type: "text", nullable: true),
                    server_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_org_created",
                schema: "pos",
                table: "idempotency_records",
                columns: new[] { "organization_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_idempotency_org_product_type_key",
                schema: "pos",
                table: "idempotency_records",
                columns: new[] { "organization_id", "product_code", "operation_type", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "pos");
        }
    }
}
