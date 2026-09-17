using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationOnlineSupplierPaymentsCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS platform.organization_online_supplier_payments_capabilities (
                    organization_id uuid NOT NULL,
                    status character varying(64) NOT NULL DEFAULT 'Disabled',
                    updated_at_utc timestamp with time zone NOT NULL,
                    updated_by_actor_reference character varying(256) NULL,
                    reason character varying(1000) NULL,
                    CONSTRAINT "PK_organization_online_supplier_payments_capabilities"
                        PRIMARY KEY (organization_id),
                    CONSTRAINT "FK_org_online_supplier_payments_capabilities_organizations"
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
                DROP TABLE IF EXISTS platform.organization_online_supplier_payments_capabilities;
                """);
        }
    }
}
