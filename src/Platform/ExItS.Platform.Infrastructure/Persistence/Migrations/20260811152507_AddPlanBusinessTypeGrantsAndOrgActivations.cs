using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanBusinessTypeGrantsAndOrgActivations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_business_type_activations",
                schema: "platform",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_business_type_activations", x => new { x.organization_id, x.business_type_id });
                    table.ForeignKey(
                        name: "FK_organization_business_type_activations_business_types_busin~",
                        column: x => x.business_type_id,
                        principalSchema: "catalog",
                        principalTable: "business_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_business_type_activations_organizations_organi~",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_version_business_type_grants",
                schema: "platform",
                columns: table => new
                {
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_type_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_version_business_type_grants", x => new { x.plan_version_id, x.business_type_id });
                    table.ForeignKey(
                        name: "FK_plan_version_business_type_grants_business_types_business_t~",
                        column: x => x.business_type_id,
                        principalSchema: "catalog",
                        principalTable: "business_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_version_business_type_grants_plan_versions_plan_versio~",
                        column: x => x.plan_version_id,
                        principalSchema: "platform",
                        principalTable: "plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organization_business_type_activations_business_type_id",
                schema: "platform",
                table: "organization_business_type_activations",
                column: "business_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_version_business_type_grants_business_type_id",
                schema: "platform",
                table: "plan_version_business_type_grants",
                column: "business_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_business_type_activations",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "plan_version_business_type_grants",
                schema: "platform");
        }
    }
}
