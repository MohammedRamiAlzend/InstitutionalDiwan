using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Document_Date_And_External_Reference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentDate",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "ExternalDocumentDate",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "ExternalReferenceNumber",
                table: "DOCUMENTS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentDate",
                table: "DOCUMENTS",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalDocumentDate",
                table: "DOCUMENTS",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalReferenceNumber",
                table: "DOCUMENTS",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
