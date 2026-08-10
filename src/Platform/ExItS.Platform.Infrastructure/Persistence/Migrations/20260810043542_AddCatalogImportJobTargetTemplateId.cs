using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportJobTargetTemplateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "target_template_id",
                schema: "catalog",
                table: "catalog_import_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_jobs_target_template_id",
                schema: "catalog",
                table: "catalog_import_jobs",
                column: "target_template_id");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_import_jobs_catalog_templates_target_template_id",
                schema: "catalog",
                table: "catalog_import_jobs",
                column: "target_template_id",
                principalSchema: "catalog",
                principalTable: "catalog_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_import_jobs_catalog_templates_target_template_id",
                schema: "catalog",
                table: "catalog_import_jobs");

            migrationBuilder.DropIndex(
                name: "ix_catalog_import_jobs_target_template_id",
                schema: "catalog",
                table: "catalog_import_jobs");

            migrationBuilder.DropColumn(
                name: "target_template_id",
                schema: "catalog",
                table: "catalog_import_jobs");
        }
    }
}
