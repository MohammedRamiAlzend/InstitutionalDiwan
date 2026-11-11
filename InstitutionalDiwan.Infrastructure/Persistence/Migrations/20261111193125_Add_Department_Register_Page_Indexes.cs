using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Department_Register_Page_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_REGISTRY_VISITS_SchemaVersionId",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropIndex(
                name: "IX_DEPARTMENT_RECORD_VALUES_DocumentFieldId",
                table: "DEPARTMENT_RECORD_VALUES");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_REG_VISITS_VERSION_NUMBER_DATE",
                table: "DOCUMENT_REGISTRY_VISITS",
                columns: new[] { "SchemaVersionId", "DocumentNumberId", "RegistryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DEPT_RECORD_VALUES_FIELD_VISIT",
                table: "DEPARTMENT_RECORD_VALUES",
                columns: new[] { "DocumentFieldId", "VisitId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DOC_REG_VISITS_VERSION_NUMBER_DATE",
                table: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropIndex(
                name: "IX_DEPT_RECORD_VALUES_FIELD_VISIT",
                table: "DEPARTMENT_RECORD_VALUES");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_REGISTRY_VISITS_SchemaVersionId",
                table: "DOCUMENT_REGISTRY_VISITS",
                column: "SchemaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_RECORD_VALUES_DocumentFieldId",
                table: "DEPARTMENT_RECORD_VALUES",
                column: "DocumentFieldId");
        }
    }
}
