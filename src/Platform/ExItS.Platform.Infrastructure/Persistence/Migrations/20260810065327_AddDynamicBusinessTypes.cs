using System;
using ExItS.Platform.Domain.GlobalCatalog;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicBusinessTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_types",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    icon_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_business_types_sort_order",
                schema: "catalog",
                table: "business_types",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ix_business_types_status",
                schema: "catalog",
                table: "business_types",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_business_types_code",
                schema: "catalog",
                table: "business_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_business_types_normalized_name",
                schema: "catalog",
                table: "business_types",
                column: "normalized_name",
                unique: true);

            var seedUtc = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
            foreach (var (id, code, name, sortOrder) in LegacyBusinessTypeSeeds.All)
            {
                migrationBuilder.InsertData(
                    schema: "catalog",
                    table: "business_types",
                    columns: new[]
                    {
                        "id", "code", "name", "normalized_name", "description", "status",
                        "sort_order", "icon_reference", "created_at_utc", "updated_at_utc"
                    },
                    values: new object[]
                    {
                        id,
                        code,
                        name,
                        name.ToUpperInvariant(),
                        null!,
                        nameof(BusinessTypeStatus.Active),
                        sortOrder,
                        null!,
                        seedUtc,
                        seedUtc
                    });
            }

            // --- category join: add id column, backfill, swap PK ---
            migrationBuilder.DropPrimaryKey(
                name: "PK_global_category_business_types",
                schema: "catalog",
                table: "global_category_business_types");

            migrationBuilder.DropIndex(
                name: "ix_global_category_business_types_type",
                schema: "catalog",
                table: "global_category_business_types");

            migrationBuilder.AddColumn<Guid>(
                name: "business_type_id",
                schema: "catalog",
                table: "global_category_business_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.global_category_business_types AS j
                SET business_type_id = bt.id
                FROM catalog.business_types AS bt
                WHERE lower(j.business_type) = lower(bt.code);
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog.global_category_business_types
                WHERE business_type_id IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "business_type",
                schema: "catalog",
                table: "global_category_business_types");

            migrationBuilder.AlterColumn<Guid>(
                name: "business_type_id",
                schema: "catalog",
                table: "global_category_business_types",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_global_category_business_types",
                schema: "catalog",
                table: "global_category_business_types",
                columns: new[] { "category_id", "business_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_global_category_business_types_type",
                schema: "catalog",
                table: "global_category_business_types",
                column: "business_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_global_category_business_types_business_types_business_type~",
                schema: "catalog",
                table: "global_category_business_types",
                column: "business_type_id",
                principalSchema: "catalog",
                principalTable: "business_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // --- product join ---
            migrationBuilder.DropPrimaryKey(
                name: "PK_global_product_business_types",
                schema: "catalog",
                table: "global_product_business_types");

            migrationBuilder.DropIndex(
                name: "ix_global_product_business_types_type",
                schema: "catalog",
                table: "global_product_business_types");

            migrationBuilder.AddColumn<Guid>(
                name: "business_type_id",
                schema: "catalog",
                table: "global_product_business_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.global_product_business_types AS j
                SET business_type_id = bt.id
                FROM catalog.business_types AS bt
                WHERE lower(j.business_type) = lower(bt.code);
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog.global_product_business_types
                WHERE business_type_id IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "business_type",
                schema: "catalog",
                table: "global_product_business_types");

            migrationBuilder.AlterColumn<Guid>(
                name: "business_type_id",
                schema: "catalog",
                table: "global_product_business_types",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_global_product_business_types",
                schema: "catalog",
                table: "global_product_business_types",
                columns: new[] { "product_id", "business_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_global_product_business_types_type",
                schema: "catalog",
                table: "global_product_business_types",
                column: "business_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_global_product_business_types_business_types_business_type_~",
                schema: "catalog",
                table: "global_product_business_types",
                column: "business_type_id",
                principalSchema: "catalog",
                principalTable: "business_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // --- templates ---
            migrationBuilder.DropIndex(
                name: "ix_catalog_templates_primary_business_type",
                schema: "catalog",
                table: "catalog_templates");

            migrationBuilder.AddColumn<Guid>(
                name: "primary_business_type_id",
                schema: "catalog",
                table: "catalog_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.catalog_templates AS t
                SET primary_business_type_id = bt.id
                FROM catalog.business_types AS bt
                WHERE lower(t.primary_business_type) = lower(bt.code);
                """);

            // Fallback any unmapped template to GeneralRetail seed.
            migrationBuilder.Sql(
                $"""
                UPDATE catalog.catalog_templates
                SET primary_business_type_id = '{LegacyBusinessTypeSeeds.GeneralRetailId:D}'
                WHERE primary_business_type_id IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "primary_business_type",
                schema: "catalog",
                table: "catalog_templates");

            migrationBuilder.AlterColumn<Guid>(
                name: "primary_business_type_id",
                schema: "catalog",
                table: "catalog_templates",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_templates_primary_business_type_id",
                schema: "catalog",
                table: "catalog_templates",
                column: "primary_business_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_templates_business_types_primary_business_type_id",
                schema: "catalog",
                table: "catalog_templates",
                column: "primary_business_type_id",
                principalSchema: "catalog",
                principalTable: "business_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_templates_business_types_primary_business_type_id",
                schema: "catalog",
                table: "catalog_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_global_category_business_types_business_types_business_type~",
                schema: "catalog",
                table: "global_category_business_types");

            migrationBuilder.DropForeignKey(
                name: "FK_global_product_business_types_business_types_business_type_~",
                schema: "catalog",
                table: "global_product_business_types");

            migrationBuilder.DropIndex(
                name: "ix_catalog_templates_primary_business_type_id",
                schema: "catalog",
                table: "catalog_templates");

            migrationBuilder.AddColumn<string>(
                name: "primary_business_type",
                schema: "catalog",
                table: "catalog_templates",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.catalog_templates AS t
                SET primary_business_type = bt.code
                FROM catalog.business_types AS bt
                WHERE t.primary_business_type_id = bt.id;
                """);

            migrationBuilder.Sql(
                """
                UPDATE catalog.catalog_templates
                SET primary_business_type = 'GeneralRetail'
                WHERE primary_business_type IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "primary_business_type_id",
                schema: "catalog",
                table: "catalog_templates");

            migrationBuilder.AlterColumn<string>(
                name: "primary_business_type",
                schema: "catalog",
                table: "catalog_templates",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_templates_primary_business_type",
                schema: "catalog",
                table: "catalog_templates",
                column: "primary_business_type");

            // category join down
            migrationBuilder.DropPrimaryKey(
                name: "PK_global_category_business_types",
                schema: "catalog",
                table: "global_category_business_types");

            migrationBuilder.DropIndex(
                name: "ix_global_category_business_types_type",
                schema: "catalog",
                table: "global_category_business_types");

            migrationBuilder.AddColumn<string>(
                name: "business_type",
                schema: "catalog",
                table: "global_category_business_types",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.global_category_business_types AS j
                SET business_type = bt.code
                FROM catalog.business_types AS bt
                WHERE j.business_type_id = bt.id;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog.global_category_business_types
                WHERE business_type IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "business_type_id",
                schema: "catalog",
                table: "global_category_business_types");

            migrationBuilder.AlterColumn<string>(
                name: "business_type",
                schema: "catalog",
                table: "global_category_business_types",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_global_category_business_types",
                schema: "catalog",
                table: "global_category_business_types",
                columns: new[] { "category_id", "business_type" });

            migrationBuilder.CreateIndex(
                name: "ix_global_category_business_types_type",
                schema: "catalog",
                table: "global_category_business_types",
                column: "business_type");

            // product join down
            migrationBuilder.DropPrimaryKey(
                name: "PK_global_product_business_types",
                schema: "catalog",
                table: "global_product_business_types");

            migrationBuilder.DropIndex(
                name: "ix_global_product_business_types_type",
                schema: "catalog",
                table: "global_product_business_types");

            migrationBuilder.AddColumn<string>(
                name: "business_type",
                schema: "catalog",
                table: "global_product_business_types",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.global_product_business_types AS j
                SET business_type = bt.code
                FROM catalog.business_types AS bt
                WHERE j.business_type_id = bt.id;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog.global_product_business_types
                WHERE business_type IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "business_type_id",
                schema: "catalog",
                table: "global_product_business_types");

            migrationBuilder.AlterColumn<string>(
                name: "business_type",
                schema: "catalog",
                table: "global_product_business_types",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_global_product_business_types",
                schema: "catalog",
                table: "global_product_business_types",
                columns: new[] { "product_id", "business_type" });

            migrationBuilder.CreateIndex(
                name: "ix_global_product_business_types_type",
                schema: "catalog",
                table: "global_product_business_types",
                column: "business_type");

            migrationBuilder.DropTable(
                name: "business_types",
                schema: "catalog");
        }
    }
}
