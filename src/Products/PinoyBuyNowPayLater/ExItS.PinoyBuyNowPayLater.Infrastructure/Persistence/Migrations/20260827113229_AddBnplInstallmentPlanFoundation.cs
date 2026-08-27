using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBnplInstallmentPlanFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "installment_plans",
                schema: "bnpl",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_by_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_superseded = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installment_plans", x => x.id);
                    table.ForeignKey(
                        name: "FK_installment_plans_financing_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "bnpl",
                        principalTable: "financing_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bnpl_installment_plans_offer",
                        column: x => x.offer_id,
                        principalSchema: "bnpl",
                        principalTable: "financing_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installment_plan_items",
                schema: "bnpl",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installment_plan_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_installment_plan_items_installment_plans_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "bnpl",
                        principalTable: "installment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_bnpl_installment_plan_items_plan_sequence",
                schema: "bnpl",
                table: "installment_plan_items",
                columns: new[] { "plan_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_installment_plans_offer_id",
                schema: "bnpl",
                table: "installment_plans",
                column: "offer_id");

            migrationBuilder.CreateIndex(
                name: "ux_bnpl_installment_plans_application_offer_version",
                schema: "bnpl",
                table: "installment_plans",
                columns: new[] { "application_id", "offer_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "installment_plan_items",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "installment_plans",
                schema: "bnpl");
        }
    }
}
