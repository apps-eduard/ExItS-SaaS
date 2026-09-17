using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Utang Check repayment metadata + clearing lifecycle; business repayments table.
/// Pending checks do not settle outstanding until Cleared.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260915180000_AddUtangCheckRepayments")]
public partial class AddUtangCheckRepayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "payment_method",
            schema: "pos",
            table: "repayments",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Cash");

        migrationBuilder.AddColumn<string>(
            name: "check_number",
            schema: "pos",
            table: "repayments",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "bank_name",
            schema: "pos",
            table: "repayments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "check_date",
            schema: "pos",
            table: "repayments",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "account_name",
            schema: "pos",
            table: "repayments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "reference",
            schema: "pos",
            table: "repayments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "check_clearing_status",
            schema: "pos",
            table: "repayments",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "None");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "cleared_at_utc",
            schema: "pos",
            table: "repayments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "cleared_by",
            schema: "pos",
            table: "repayments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "bounced_at_utc",
            schema: "pos",
            table: "repayments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "bounced_by",
            schema: "pos",
            table: "repayments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "bounce_reason",
            schema: "pos",
            table: "repayments",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "cancelled_at_utc",
            schema: "pos",
            table: "repayments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "cancelled_by",
            schema: "pos",
            table: "repayments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "cancel_reason",
            schema: "pos",
            table: "repayments",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_repayments_payment_method",
            schema: "pos",
            table: "repayments",
            sql: "payment_method IN ('Cash', 'ManualGCash', 'Check')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_repayments_check_clearing_status",
            schema: "pos",
            table: "repayments",
            sql: "check_clearing_status IN ('None', 'PendingClearing', 'Cleared', 'Bounced', 'Cancelled')");

        migrationBuilder.CreateIndex(
            name: "ix_repayments_org_customer_check_clearing",
            schema: "pos",
            table: "repayments",
            columns: new[] { "organization_id", "customer_id", "check_clearing_status" });

        migrationBuilder.CreateTable(
            name: "business_repayments",
            schema: "pos",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                seller_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                buyer_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                remarks = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                payment_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                check_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                bank_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                check_date = table.Column<DateOnly>(type: "date", nullable: true),
                account_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                check_clearing_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                reversed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                reversal_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                reversed_by = table.Column<Guid>(type: "uuid", nullable: true),
                cleared_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cleared_by = table.Column<Guid>(type: "uuid", nullable: true),
                bounced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                bounced_by = table.Column<Guid>(type: "uuid", nullable: true),
                bounce_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                cancel_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_business_repayments", x => x.id);
                table.CheckConstraint("ck_business_repayments_status", "status IN ('Active', 'Reversed')");
                table.CheckConstraint("ck_business_repayments_amount_positive", "amount > 0");
                table.CheckConstraint(
                    "ck_business_repayments_reversal_consistency",
                    "(status = 'Active' AND reversed_at_utc IS NULL AND reversal_reason IS NULL AND reversed_by IS NULL) OR (status = 'Reversed' AND reversed_at_utc IS NOT NULL AND reversal_reason IS NOT NULL AND reversed_by IS NOT NULL)");
                table.CheckConstraint(
                    "ck_business_repayments_payment_method",
                    "payment_method IN ('Cash', 'ManualGCash', 'Check')");
                table.CheckConstraint(
                    "ck_business_repayments_check_clearing_status",
                    "check_clearing_status IN ('None', 'PendingClearing', 'Cleared', 'Bounced', 'Cancelled')");
            });

        migrationBuilder.CreateIndex(
            name: "ix_business_repayments_seller_buyer_recorded",
            schema: "pos",
            table: "business_repayments",
            columns: new[] { "seller_organization_id", "buyer_organization_id", "recorded_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_business_repayments_seller_buyer_status",
            schema: "pos",
            table: "business_repayments",
            columns: new[] { "seller_organization_id", "buyer_organization_id", "status", "check_clearing_status" });

        migrationBuilder.CreateIndex(
            name: "ix_business_repayments_connection_id",
            schema: "pos",
            table: "business_repayments",
            column: "connection_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "business_repayments", schema: "pos");

        migrationBuilder.DropIndex(
            name: "ix_repayments_org_customer_check_clearing",
            schema: "pos",
            table: "repayments");

        migrationBuilder.DropCheckConstraint(
            name: "ck_repayments_payment_method",
            schema: "pos",
            table: "repayments");

        migrationBuilder.DropCheckConstraint(
            name: "ck_repayments_check_clearing_status",
            schema: "pos",
            table: "repayments");

        migrationBuilder.DropColumn(name: "payment_method", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "check_number", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "bank_name", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "check_date", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "account_name", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "reference", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "check_clearing_status", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "cleared_at_utc", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "cleared_by", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "bounced_at_utc", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "bounced_by", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "bounce_reason", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "cancelled_at_utc", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "cancelled_by", schema: "pos", table: "repayments");
        migrationBuilder.DropColumn(name: "cancel_reason", schema: "pos", table: "repayments");
    }
}
