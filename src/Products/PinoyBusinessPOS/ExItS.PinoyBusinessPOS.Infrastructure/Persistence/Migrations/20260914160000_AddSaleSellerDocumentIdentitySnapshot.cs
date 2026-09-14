using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSaleSellerDocumentIdentitySnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "seller_document_identity_json",
            table: "sales",
            type: "character varying(4000)",
            maxLength: 4000,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "seller_document_identity_json",
            table: "sales");
    }
}
