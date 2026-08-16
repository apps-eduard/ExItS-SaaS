using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBirRegistrationReadinessProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "registered_taxpayer_name",
                schema: "platform",
                table: "organization_compliance_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "setup_status",
                schema: "platform",
                table: "organization_compliance_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "NotConfigured");

            migrationBuilder.AddColumn<string>(
                name: "tin_normalized",
                schema: "platform",
                table: "organization_compliance_profiles",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "branch_compliance_profiles",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bir_branch_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    setup_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "NotConfigured"),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_actor_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_compliance_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_compliance_profiles_organization_branches_organizati~",
                        column: x => x.organization_branch_id,
                        principalSchema: "platform",
                        principalTable: "organization_branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_compliance_profiles_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "compliance_registration_records",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    document_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    issued_at = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_at = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    review_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_registration_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_compliance_registration_records_organization_branches_organ~",
                        column: x => x.organization_branch_id,
                        principalSchema: "platform",
                        principalTable: "organization_branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_compliance_registration_records_organizations_organization_~",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_compliance_profiles_org",
                schema: "platform",
                table: "branch_compliance_profiles",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "UX_branch_compliance_profiles_branch",
                schema: "platform",
                table: "branch_compliance_profiles",
                column: "organization_branch_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compliance_registration_records_org",
                schema: "platform",
                table: "compliance_registration_records",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_registration_records_organization_branch_id",
                schema: "platform",
                table: "compliance_registration_records",
                column: "organization_branch_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_compliance_profiles",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "compliance_registration_records",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "registered_taxpayer_name",
                schema: "platform",
                table: "organization_compliance_profiles");

            migrationBuilder.DropColumn(
                name: "setup_status",
                schema: "platform",
                table: "organization_compliance_profiles");

            migrationBuilder.DropColumn(
                name: "tin_normalized",
                schema: "platform",
                table: "organization_compliance_profiles");
        }
    }
}
