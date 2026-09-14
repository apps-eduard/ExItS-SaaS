using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seller-owned contact fields on ConnectedSupplierRelationship (Business Customer profile).
/// Does not alter buyer Organization identity snapshots.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260914120000_AddConnectedSupplierRelationshipContact")]
public partial class AddConnectedSupplierRelationshipContact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "contact_person_name",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "contact_department",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "contact_role",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "contact_phone",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "contact_email",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "preferred_contact_method",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "delivery_instructions",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "billing_contact_notes",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "internal_notes",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "contact_person_name",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "contact_department",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "contact_role",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "contact_phone",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "contact_email",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "preferred_contact_method",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "delivery_instructions",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "billing_contact_notes",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "internal_notes",
            schema: "pos",
            table: "connected_supplier_relationships");
    }
}
