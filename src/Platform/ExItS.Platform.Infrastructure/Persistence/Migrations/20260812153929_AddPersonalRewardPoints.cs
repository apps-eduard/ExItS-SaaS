using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalRewardPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reward_points_price",
                schema: "platform",
                table: "personal_feature_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "personal_reward_balances",
                schema: "platform",
                columns: table => new
                {
                    personal_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    available_points = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_reward_balances", x => x.personal_user_id);
                    table.ForeignKey(
                        name: "FK_personal_reward_balances_platform_users_personal_user_id",
                        column: x => x.personal_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_reward_transactions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    personal_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    signed_delta = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    reference_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_reward_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_reward_transactions_platform_users_personal_user_id",
                        column: x => x.personal_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_personal_reward_transactions_user_created",
                schema: "platform",
                table: "personal_reward_transactions",
                columns: new[] { "personal_user_id", "created_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_personal_reward_transactions_user_idempotency",
                schema: "platform",
                table: "personal_reward_transactions",
                columns: new[] { "personal_user_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            // Development/test default price for the WP06 feature (not a production launch price).
            migrationBuilder.Sql(
                """
                UPDATE platform.personal_feature_definitions
                SET reward_points_price = 100
                WHERE feature_code = 'personal-digital-records-extended'
                  AND reward_points_price IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_reward_balances",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_reward_transactions",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "reward_points_price",
                schema: "platform",
                table: "personal_feature_definitions");
        }
    }
}
