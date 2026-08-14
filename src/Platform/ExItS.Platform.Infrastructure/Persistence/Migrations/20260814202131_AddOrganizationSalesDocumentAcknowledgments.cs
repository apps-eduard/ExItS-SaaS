using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSalesDocumentAcknowledgments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS platform.organization_sales_document_acknowledgments (
                    id uuid NOT NULL,
                    organization_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    version character varying(128) NOT NULL,
                    acknowledged_at_utc timestamp with time zone NOT NULL,
                    content_key character varying(128) NULL,
                    CONSTRAINT "PK_organization_sales_document_acknowledgments" PRIMARY KEY (id),
                    CONSTRAINT "FK_sales_document_ack_org"
                        FOREIGN KEY (organization_id) REFERENCES platform.organizations (id) ON DELETE CASCADE,
                    CONSTRAINT "FK_sales_document_ack_user"
                        FOREIGN KEY (user_id) REFERENCES platform.platform_users (id) ON DELETE RESTRICT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_sales_document_ack_org_user_version"
                    ON platform.organization_sales_document_acknowledgments
                    (organization_id, user_id, version);

                CREATE INDEX IF NOT EXISTS "IX_sales_document_ack_user"
                    ON platform.organization_sales_document_acknowledgments (user_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS platform.organization_sales_document_acknowledgments;
                """);
        }
    }
}
