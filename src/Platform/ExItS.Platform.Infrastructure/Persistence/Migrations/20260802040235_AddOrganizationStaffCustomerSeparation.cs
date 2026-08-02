using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationStaffCustomerSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_customers",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    owning_product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    linked_user_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_business_customers_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_customers",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_customers_business_customers_business_customer_id",
                        column: x => x.business_customer_id,
                        principalSchema: "platform",
                        principalTable: "business_customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_customers_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_link_requests",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    declined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_link_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_link_requests_business_customers_business_customer~",
                        column: x => x.business_customer_id,
                        principalSchema: "platform",
                        principalTable: "business_customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_link_requests_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linked_customer_app_users",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_link_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    linked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linked_customer_app_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_linked_customer_app_users_business_customers_business_custo~",
                        column: x => x.business_customer_id,
                        principalSchema: "platform",
                        principalTable: "business_customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linked_customer_app_users_customer_link_requests_source_lin~",
                        column: x => x.source_link_request_id,
                        principalSchema: "platform",
                        principalTable: "customer_link_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linked_customer_app_users_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linked_customer_app_users_platform_users_user_identity_id",
                        column: x => x.user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_business_customers_org_product",
                schema: "platform",
                table: "business_customers",
                columns: new[] { "organization_id", "owning_product_code" });

            migrationBuilder.CreateIndex(
                name: "ix_business_customers_organization_id",
                schema: "platform",
                table: "business_customers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_customers_organization_id",
                schema: "platform",
                table: "credit_customers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_credit_customers_active_business_customer",
                schema: "platform",
                table: "credit_customers",
                column: "business_customer_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_customer_link_requests_organization_id",
                schema: "platform",
                table: "customer_link_requests",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_customer_link_requests_pending_customer",
                schema: "platform",
                table: "customer_link_requests",
                column: "business_customer_id",
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ux_customer_link_requests_token_hash",
                schema: "platform",
                table: "customer_link_requests",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_linked_customer_app_users_organization_id",
                schema: "platform",
                table: "linked_customer_app_users",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_linked_customer_app_users_source_link_request_id",
                schema: "platform",
                table: "linked_customer_app_users",
                column: "source_link_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_linked_customer_app_users_user_identity_id",
                schema: "platform",
                table: "linked_customer_app_users",
                column: "user_identity_id");

            migrationBuilder.CreateIndex(
                name: "ux_linked_customer_app_users_active_customer",
                schema: "platform",
                table: "linked_customer_app_users",
                column: "business_customer_id",
                unique: true,
                filter: "status = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_customers",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "linked_customer_app_users",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "customer_link_requests",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "business_customers",
                schema: "platform");
        }
    }
}
