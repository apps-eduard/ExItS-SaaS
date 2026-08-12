using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalRewardClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personal_reward_claims",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    personal_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    claim_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    points_awarded = table.Column<int>(type: "integer", nullable: false),
                    reward_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_reward_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_reward_claims_platform_users_personal_user_id",
                        column: x => x.personal_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_personal_reward_claims_transaction",
                schema: "platform",
                table: "personal_reward_claims",
                column: "reward_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_personal_reward_claims_user_type_key",
                schema: "platform",
                table: "personal_reward_claims",
                columns: new[] { "personal_user_id", "claim_type", "claim_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_reward_claims",
                schema: "platform");
        }
    }
}
