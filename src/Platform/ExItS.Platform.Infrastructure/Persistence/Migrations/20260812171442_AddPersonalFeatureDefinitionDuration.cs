using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalFeatureDefinitionDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "default_entitlement_duration_days",
                schema: "platform",
                table: "personal_feature_definitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_personal_feature_definitions_duration_days",
                schema: "platform",
                table: "personal_feature_definitions",
                sql: "default_entitlement_duration_days IS NULL OR (default_entitlement_duration_days >= 1 AND default_entitlement_duration_days <= 3650)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_personal_feature_definitions_duration_days",
                schema: "platform",
                table: "personal_feature_definitions");

            migrationBuilder.DropColumn(
                name: "default_entitlement_duration_days",
                schema: "platform",
                table: "personal_feature_definitions");
        }
    }
}
