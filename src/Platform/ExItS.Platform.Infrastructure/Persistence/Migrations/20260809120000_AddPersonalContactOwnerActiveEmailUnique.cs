using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PlatformDbContext))]
[Migration("20260809120000_AddPersonalContactOwnerActiveEmailUnique")]
public partial class AddPersonalContactOwnerActiveEmailUnique : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Align stored emails with PersonalContact.NormalizeOptionalEmail (trim + upper).
        migrationBuilder.Sql(
            """
            UPDATE platform.personal_contacts
            SET email = upper(btrim(email))
            WHERE email IS NOT NULL;
            """);

        // Keep the earliest active contact per owner+email; clear email on later duplicates
        // so the unique filter index can be created safely.
        migrationBuilder.Sql(
            """
            WITH ranked AS (
                SELECT id,
                       row_number() OVER (
                           PARTITION BY owner_user_identity_id, email
                           ORDER BY created_at_utc ASC, id ASC) AS rn
                FROM platform.personal_contacts
                WHERE email IS NOT NULL
                  AND status = 'Active'
            )
            UPDATE platform.personal_contacts AS c
            SET email = NULL,
                updated_at_utc = now() AT TIME ZONE 'utc'
            FROM ranked
            WHERE c.id = ranked.id
              AND ranked.rn > 1;
            """);

        migrationBuilder.CreateIndex(
            name: "ux_personal_contacts_owner_active_email",
            schema: "platform",
            table: "personal_contacts",
            columns: new[] { "owner_user_identity_id", "email" },
            unique: true,
            filter: "email IS NOT NULL AND status = 'Active'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_personal_contacts_owner_active_email",
            schema: "platform",
            table: "personal_contacts");
    }
}
