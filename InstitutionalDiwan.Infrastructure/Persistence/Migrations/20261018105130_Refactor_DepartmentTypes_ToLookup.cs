using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_DepartmentTypes_ToLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DEPARTMENTS_DEPARTMENT_TYPES_DepartmentTypeCode",
                table: "DEPARTMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DEPARTMENTS_TYPE_ACTIVE",
                table: "DEPARTMENTS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DEPARTMENT_TYPES",
                table: "DEPARTMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "DepartmentTypeCode",
                table: "DEPARTMENTS");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DEPARTMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "DEPARTMENT_TYPES");

            migrationBuilder.RenameColumn(
                name: "NameAr",
                table: "DEPARTMENT_TYPES",
                newName: "DescAr");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentTypeId",
                table: "DEPARTMENTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DEPARTMENT_TYPES",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "DEPARTMENT_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DEPARTMENT_TYPES",
                table: "DEPARTMENT_TYPES",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENTS_TYPE_ACTIVE",
                table: "DEPARTMENTS",
                columns: new[] { "DepartmentTypeId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_DEPARTMENTS_DEPARTMENT_TYPES_DepartmentTypeId",
                table: "DEPARTMENTS",
                column: "DepartmentTypeId",
                principalTable: "DEPARTMENT_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DEPARTMENTS_DEPARTMENT_TYPES_DepartmentTypeId",
                table: "DEPARTMENTS");

            migrationBuilder.DropIndex(
                name: "IX_DEPARTMENTS_TYPE_ACTIVE",
                table: "DEPARTMENTS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DEPARTMENT_TYPES",
                table: "DEPARTMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "DepartmentTypeId",
                table: "DEPARTMENTS");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DEPARTMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "Desc",
                table: "DEPARTMENT_TYPES");

            migrationBuilder.RenameColumn(
                name: "DescAr",
                table: "DEPARTMENT_TYPES",
                newName: "NameAr");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentTypeCode",
                table: "DEPARTMENTS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DEPARTMENT_TYPES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "DEPARTMENT_TYPES",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DEPARTMENT_TYPES",
                table: "DEPARTMENT_TYPES",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENTS_TYPE_ACTIVE",
                table: "DEPARTMENTS",
                columns: new[] { "DepartmentTypeCode", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_DEPARTMENTS_DEPARTMENT_TYPES_DepartmentTypeCode",
                table: "DEPARTMENTS",
                column: "DepartmentTypeCode",
                principalTable: "DEPARTMENT_TYPES",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
