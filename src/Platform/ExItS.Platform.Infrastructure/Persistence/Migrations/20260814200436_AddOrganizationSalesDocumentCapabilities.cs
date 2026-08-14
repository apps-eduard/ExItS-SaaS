using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSalesDocumentCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS platform.organization_sales_document_capabilities (
                    organization_id uuid NOT NULL,
                    tax_document_issuance_enabled boolean NOT NULL DEFAULT FALSE,
                    updated_at_utc timestamp with time zone NOT NULL,
                    updated_by_actor_reference character varying(256) NULL,
                    CONSTRAINT "PK_organization_sales_document_capabilities"
                        PRIMARY KEY (organization_id),
                    CONSTRAINT "FK_org_sales_document_capabilities_organizations"
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
                DROP TABLE IF EXISTS platform.organization_sales_document_capabilities;
                """);
        }
    }
}
