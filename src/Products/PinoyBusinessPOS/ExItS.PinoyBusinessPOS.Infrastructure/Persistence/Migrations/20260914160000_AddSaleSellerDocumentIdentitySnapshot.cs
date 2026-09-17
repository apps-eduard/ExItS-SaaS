using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Sale-time seller document identity snapshot for customer-facing purchase summaries.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260914160000_AddSaleSellerDocumentIdentitySnapshot")]
public partial class AddSaleSellerDocumentIdentitySnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "seller_document_identity_json",
            schema: "pos",
            table: "sales",
            type: "character varying(4000)",
            maxLength: 4000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "seller_document_identity_json",
            schema: "pos",
            table: "sales");
    }
}
