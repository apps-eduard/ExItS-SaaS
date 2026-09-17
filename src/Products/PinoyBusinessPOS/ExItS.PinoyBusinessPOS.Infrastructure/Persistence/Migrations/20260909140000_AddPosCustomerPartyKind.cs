using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds party_kind (Person/Business) on POS customers. Existing rows default to Person.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260909140000_AddPosCustomerPartyKind")]
public partial class AddPosCustomerPartyKind : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "party_kind",
            schema: "pos",
            table: "customers",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Person");

        migrationBuilder.AddCheckConstraint(
            name: "ck_customers_party_kind",
            schema: "pos",
            table: "customers",
            sql: "party_kind IN ('Person', 'Business')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_customers_party_kind",
            schema: "pos",
            table: "customers");

        migrationBuilder.DropColumn(
            name: "party_kind",
            schema: "pos",
            table: "customers");
    }
}
