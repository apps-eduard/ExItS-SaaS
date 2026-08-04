using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCatalogImportMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "catalog_imported_at",
                schema: "pos",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "catalog_snapshot_version",
                schema: "pos",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "catalog_source",
                schema: "pos",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<Guid>(
                name: "platform_global_product_id",
                schema: "pos",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "platform_template_id",
                schema: "pos",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_global_category_id",
                schema: "pos",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_global_category_id",
                schema: "pos",
                table: "product_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalog_import_jobs",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    platform_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    batch_number = table.Column<int>(type: "integer", nullable: true),
                    catalog_source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    processed_count = table.Column<int>(type: "integer", nullable: false),
                    imported_count = table.Column<int>(type: "integer", nullable: false),
                    skipped_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    current_stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    error_summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_import_jobs", x => x.id);
                    table.CheckConstraint("ck_catalog_import_jobs_kind", "job_kind IN ('TemplateBatch', 'SelectedProducts')");
                    table.CheckConstraint("ck_catalog_import_jobs_source", "catalog_source IN ('Manual', 'Template', 'GlobalSearch', 'BulkImport')");
                    table.CheckConstraint("ck_catalog_import_jobs_status", "status IN ('Queued', 'Processing', 'Completed', 'CompletedWithWarnings', 'Failed', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "catalog_import_items",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_import_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_global_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    barcode = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    suggested_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    source_global_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_category_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    local_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    error_message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_import_items", x => x.id);
                    table.CheckConstraint("ck_catalog_import_items_status", "status IN ('Pending', 'Imported', 'Skipped', 'Failed')");
                    table.ForeignKey(
                        name: "fk_catalog_import_items_jobs",
                        column: x => x.catalog_import_job_id,
                        principalSchema: "pos",
                        principalTable: "catalog_import_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_products_org_platform_global_product",
                schema: "pos",
                table: "products",
                columns: new[] { "organization_id", "platform_global_product_id" },
                unique: true,
                filter: "platform_global_product_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_catalog_source",
                schema: "pos",
                table: "products",
                sql: "catalog_source IN ('Manual', 'Template', 'GlobalSearch', 'BulkImport')");

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_org_source_global",
                schema: "pos",
                table: "product_categories",
                columns: new[] { "organization_id", "source_global_category_id" },
                filter: "source_global_category_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_items_job_sort",
                schema: "pos",
                table: "catalog_import_items",
                columns: new[] { "catalog_import_job_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_items_job_status",
                schema: "pos",
                table: "catalog_import_items",
                columns: new[] { "catalog_import_job_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_jobs_org_created",
                schema: "pos",
                table: "catalog_import_jobs",
                columns: new[] { "organization_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_jobs_status_heartbeat",
                schema: "pos",
                table: "catalog_import_jobs",
                columns: new[] { "status", "last_heartbeat_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_import_jobs_org_idempotency",
                schema: "pos",
                table: "catalog_import_jobs",
                columns: new[] { "organization_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_import_items",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "catalog_import_jobs",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ux_products_org_platform_global_product",
                schema: "pos",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_catalog_source",
                schema: "pos",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_product_categories_org_source_global",
                schema: "pos",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "catalog_imported_at",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "catalog_snapshot_version",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "catalog_source",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "platform_global_product_id",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "platform_template_id",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "source_global_category_id",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "source_global_category_id",
                schema: "pos",
                table: "product_categories");
        }
    }
}
