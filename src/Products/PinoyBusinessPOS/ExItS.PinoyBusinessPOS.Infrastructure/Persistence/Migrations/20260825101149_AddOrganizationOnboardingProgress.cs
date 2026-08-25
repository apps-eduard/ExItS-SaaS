using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationOnboardingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_onboarding_progress",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_setup_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    business_setup_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    product_template_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    overall_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    primary_business_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_onboarding_progress", x => x.organization_id);
                    table.CheckConstraint("ck_organization_onboarding_progress_business_setup_status", "business_setup_status IN ('NotStarted', 'Completed', 'Skipped')");
                    table.CheckConstraint("ck_organization_onboarding_progress_organization_setup_status", "organization_setup_status IN ('NotStarted', 'Completed', 'Skipped')");
                    table.CheckConstraint("ck_organization_onboarding_progress_overall_status", "overall_status IN ('InProgress', 'Completed', 'FinishedLater')");
                    table.CheckConstraint("ck_organization_onboarding_progress_product_template_status", "product_template_status IN ('NotStarted', 'Completed', 'Skipped')");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_onboarding_progress",
                schema: "pos");
        }
    }
}
