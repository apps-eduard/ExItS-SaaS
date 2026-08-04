using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalCatalogImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_import_jobs",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    file_format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    file_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    requested_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    processed_count = table.Column<int>(type: "integer", nullable: false),
                    imported_count = table.Column<int>(type: "integer", nullable: false),
                    skipped_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    current_stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    error_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_import_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_import_items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_import_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    global_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    suggested_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    suggested_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    image_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    search_tags_raw = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    business_types_raw = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_global_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_import_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_import_items_catalog_import_jobs_catalog_import_job~",
                        column: x => x.catalog_import_job_id,
                        principalSchema: "catalog",
                        principalTable: "catalog_import_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_items_job_row",
                schema: "catalog",
                table: "catalog_import_items",
                columns: new[] { "catalog_import_job_id", "row_number" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_items_job_status",
                schema: "catalog",
                table: "catalog_import_items",
                columns: new[] { "catalog_import_job_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_jobs_created_at",
                schema: "catalog",
                table: "catalog_import_jobs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_jobs_status",
                schema: "catalog",
                table: "catalog_import_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_import_jobs_idempotency_key",
                schema: "catalog",
                table: "catalog_import_jobs",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_import_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_import_jobs",
                schema: "catalog");
        }
    }
}
