using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CentralDiwanDepartmentId",
                table: "INSTITUTION_SETTINGS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleEmployeeId",
                table: "INSTITUTION_SETTINGS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_SETTINGS_CENTRAL_DIWAN",
                table: "INSTITUTION_SETTINGS",
                column: "CentralDiwanDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_SETTINGS_ResponsibleEmployeeId",
                table: "INSTITUTION_SETTINGS",
                column: "ResponsibleEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_SETTINGS_DEPARTMENTS_CentralDiwanDepartmentId",
                table: "INSTITUTION_SETTINGS",
                column: "CentralDiwanDepartmentId",
                principalTable: "DEPARTMENTS",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_SETTINGS_EMPLOYEES_ResponsibleEmployeeId",
                table: "INSTITUTION_SETTINGS",
                column: "ResponsibleEmployeeId",
                principalTable: "EMPLOYEES",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_SETTINGS_DEPARTMENTS_CentralDiwanDepartmentId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_SETTINGS_EMPLOYEES_ResponsibleEmployeeId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropIndex(
                name: "IX_INSTITUTION_SETTINGS_CENTRAL_DIWAN",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropIndex(
                name: "IX_INSTITUTION_SETTINGS_ResponsibleEmployeeId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropColumn(
                name: "CentralDiwanDepartmentId",
                table: "INSTITUTION_SETTINGS");

            migrationBuilder.DropColumn(
                name: "ResponsibleEmployeeId",
                table: "INSTITUTION_SETTINGS");
        }
    }
}
