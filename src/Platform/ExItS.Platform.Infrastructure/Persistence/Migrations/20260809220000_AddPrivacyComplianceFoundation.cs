using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyComplianceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "privacy_compliance_evidence",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reference_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privacy_compliance_evidence", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "privacy_compliance_processing_systems",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    system_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purpose = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    data_subjects = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    personal_data_categories = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    sensitive_data_categories = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    storage_location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    recipients_processors = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    retention_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    security_controls = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    owner = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    pia_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privacy_compliance_processing_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "privacy_compliance_requirements",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    requirement_level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_reviewed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_review_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    source_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    requires_dpo_legal_verification = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privacy_compliance_requirements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_compliance_evidence_requirement_id",
                schema: "platform",
                table: "privacy_compliance_evidence",
                column: "requirement_id");

            migrationBuilder.CreateIndex(
                name: "ux_privacy_compliance_evidence_requirement_path",
                schema: "platform",
                table: "privacy_compliance_evidence",
                columns: new[] { "requirement_id", "reference_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_privacy_compliance_processing_systems_code",
                schema: "platform",
                table: "privacy_compliance_processing_systems",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_privacy_compliance_requirements_code",
                schema: "platform",
                table: "privacy_compliance_requirements",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "privacy_compliance_evidence",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "privacy_compliance_processing_systems",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "privacy_compliance_requirements",
                schema: "platform");
        }
    }
}
