using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Archive_Diwan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ArchiveDiwanDepartmentId",
                table: "INSTITUTION_SETTINGS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_SETTINGS_ARCHIVE_DIWAN",
                table: "INSTITUTION_SETTINGS",
                column: "ArchiveDiwanDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_SETTINGS_DEPARTMENTS_ArchiveDiwanDepartmentId",
                table: "INSTITUTION_SETTINGS",
                column: "ArchiveDiwanDepartmentId",
                principalTable: "DEPARTMENTS",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_SETTINGS_DEPARTMENTS_ArchiveDiwanDepartmentId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropIndex(
                name: "IX_INSTITUTION_SETTINGS_ARCHIVE_DIWAN",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropColumn(
                name: "ArchiveDiwanDepartmentId",
                table: "INSTITUTION_SETTINGS");
        }
    }
}
