using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationComplianceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS platform.organization_compliance_profiles (
                    organization_id uuid NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    updated_by_actor_reference character varying(256) NULL,
                    CONSTRAINT "PK_organization_compliance_profiles"
                        PRIMARY KEY (organization_id),
                    CONSTRAINT "FK_org_compliance_profiles_organizations"
                        FOREIGN KEY (organization_id)
                        REFERENCES platform.organizations (id)
                        ON DELETE CASCADE
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS platform.organization_compliance_profiles;
                """);
        }
    }
}
