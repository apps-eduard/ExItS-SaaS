using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds immutable ledger_owner_user_identity_id (creator = sole financial writer).
/// Legacy backfill (never guess among two user participants):
/// 1) earliest financial entry CreatedByUserIdentityId (deterministic creator);
/// 2) sole user participant when the other side is contact-only (private owner ledger);
/// 3) contact OwnerUserIdentityId when no user participant remains (private contact debt).
/// Fails rather than assigning the wrong person when creator cannot be determined
/// (e.g. empty shared shell with both user participants and no entries).
/// </summary>
[DbContext(typeof(PlatformDbContext))]
[Migration("20260930140000_AddPersonalDebtLedgerOwner")]
public partial class AddPersonalDebtLedgerOwner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ledger_owner_user_identity_id",
            schema: "platform",
            table: "personal_debt_relationships",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE platform.personal_debt_relationships AS r
            SET ledger_owner_user_identity_id = first_entry.created_by_user_identity_id
            FROM (
                SELECT DISTINCT ON (e.relationship_id)
                    e.relationship_id,
                    e.created_by_user_identity_id
                FROM platform.personal_utang_entries AS e
                ORDER BY e.relationship_id, e.created_at_utc ASC, e.id ASC
            ) AS first_entry
            WHERE r.id = first_entry.relationship_id
              AND r.ledger_owner_user_identity_id IS NULL;
            """);

        // Sole user participant (other side contact-only) — do not COALESCE when both users present.
        migrationBuilder.Sql(
            """
            UPDATE platform.personal_debt_relationships
            SET ledger_owner_user_identity_id = creditor_user_identity_id
            WHERE ledger_owner_user_identity_id IS NULL
              AND creditor_user_identity_id IS NOT NULL
              AND debtor_user_identity_id IS NULL;

            UPDATE platform.personal_debt_relationships
            SET ledger_owner_user_identity_id = debtor_user_identity_id
            WHERE ledger_owner_user_identity_id IS NULL
              AND debtor_user_identity_id IS NOT NULL
              AND creditor_user_identity_id IS NULL;
            """);

        migrationBuilder.Sql(
            """
            UPDATE platform.personal_debt_relationships AS r
            SET ledger_owner_user_identity_id = c.owner_user_identity_id
            FROM platform.personal_contacts AS c
            WHERE r.ledger_owner_user_identity_id IS NULL
              AND r.creditor_user_identity_id IS NULL
              AND r.debtor_user_identity_id IS NULL
              AND (r.creditor_contact_id = c.id OR r.debtor_contact_id = c.id);
            """);

        // Fail rather than guessing creator among two user participants with no entry history.
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM platform.personal_debt_relationships
                    WHERE ledger_owner_user_identity_id IS NULL)
                THEN
                    RAISE EXCEPTION 'personal_debt_relationships.ledger_owner_user_identity_id backfill left null rows';
                END IF;
            END $$;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "ledger_owner_user_identity_id",
            schema: "platform",
            table: "personal_debt_relationships",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_personal_debt_relationships_ledger_owner_user_identity_id",
            schema: "platform",
            table: "personal_debt_relationships",
            column: "ledger_owner_user_identity_id");

        migrationBuilder.AddForeignKey(
            name: "FK_personal_debt_relationships_platform_users_ledger_owner_user_identity_id",
            schema: "platform",
            table: "personal_debt_relationships",
            column: "ledger_owner_user_identity_id",
            principalSchema: "platform",
            principalTable: "platform_users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_personal_debt_relationships_platform_users_ledger_owner_user_identity_id",
            schema: "platform",
            table: "personal_debt_relationships");

        migrationBuilder.DropIndex(
            name: "IX_personal_debt_relationships_ledger_owner_user_identity_id",
            schema: "platform",
            table: "personal_debt_relationships");

        migrationBuilder.DropColumn(
            name: "ledger_owner_user_identity_id",
            schema: "platform",
            table: "personal_debt_relationships");
    }
}
