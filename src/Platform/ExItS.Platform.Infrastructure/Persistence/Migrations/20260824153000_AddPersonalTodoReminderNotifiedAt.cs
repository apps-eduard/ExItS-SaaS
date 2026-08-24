using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PlatformDbContext))]
    [Migration("20260824153000_AddPersonalTodoReminderNotifiedAt")]
    public partial class AddPersonalTodoReminderNotifiedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reminder_notified_at_utc",
                schema: "platform",
                table: "personal_todos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_personal_todos_status_reminder",
                schema: "platform",
                table: "personal_todos",
                columns: new[] { "status", "reminder_at_utc" },
                filter: "reminder_at_utc IS NOT NULL AND reminder_notified_at_utc IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_personal_todos_status_reminder",
                schema: "platform",
                table: "personal_todos");

            migrationBuilder.DropColumn(
                name: "reminder_notified_at_utc",
                schema: "platform",
                table: "personal_todos");
        }
    }
}
