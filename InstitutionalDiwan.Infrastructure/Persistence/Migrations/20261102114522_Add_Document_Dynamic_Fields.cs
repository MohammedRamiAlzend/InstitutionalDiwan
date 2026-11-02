using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Document_Dynamic_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchemaVersionId",
                table: "DOCUMENTS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DOCUMENT_FIELDS",
                columns: table => new
                {
                    DocumentFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_FIELDS", x => x.DocumentFieldId);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_FIELDS_DOCUMENT_TYPES_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DOCUMENT_TYPES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                columns: table => new
                {
                    SchemaVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_TYPE_SCHEMA_VERSIONS", x => x.SchemaVersionId);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_TYPE_SCHEMA_VERSIONS_DOCUMENT_TYPES_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DOCUMENT_TYPES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_FIELD_VALUES",
                columns: table => new
                {
                    DocumentFieldValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NumberValue = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    OptionKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_FIELD_VALUES", x => x.DocumentFieldValueId);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_FIELD_VALUES_DOCUMENTS_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "DOCUMENTS",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_FIELD_VALUES_DOCUMENT_FIELDS_DocumentFieldId",
                        column: x => x.DocumentFieldId,
                        principalTable: "DOCUMENT_FIELDS",
                        principalColumn: "DocumentFieldId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS",
                columns: table => new
                {
                    SchemaVersionFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemaVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    RegexPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegexErrorMessageAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MinNumber = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    MaxNumber = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS", x => x.SchemaVersionFieldId);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS_DOCUMENT_FIELDS_DocumentFieldId",
                        column: x => x.DocumentFieldId,
                        principalTable: "DOCUMENT_FIELDS",
                        principalColumn: "DocumentFieldId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS_DOCUMENT_TYPE_SCHEMA_VERSIONS_SchemaVersionId",
                        column: x => x.SchemaVersionId,
                        principalTable: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                        principalColumn: "SchemaVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENTS_SCHEMA_VERSION",
                table: "DOCUMENTS",
                column: "SchemaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_FIELD_VALUES_BOOL",
                table: "DOCUMENT_FIELD_VALUES",
                columns: new[] { "DocumentFieldId", "BoolValue" },
                filter: "[BoolValue] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_FIELD_VALUES_DATE",
                table: "DOCUMENT_FIELD_VALUES",
                columns: new[] { "DocumentFieldId", "DateValue" },
                filter: "[DateValue] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_FIELD_VALUES_NUMBER",
                table: "DOCUMENT_FIELD_VALUES",
                columns: new[] { "DocumentFieldId", "NumberValue" },
                filter: "[NumberValue] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_FIELD_VALUES_OPTION",
                table: "DOCUMENT_FIELD_VALUES",
                columns: new[] { "DocumentFieldId", "OptionKey" },
                filter: "[OptionKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_FIELD_VALUES_DOC_FIELD",
                table: "DOCUMENT_FIELD_VALUES",
                columns: new[] { "DocumentId", "DocumentFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DOCUMENT_FIELDS_TYPE_KEY",
                table: "DOCUMENT_FIELDS",
                columns: new[] { "DocumentTypeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS_DocumentFieldId",
                table: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS",
                column: "DocumentFieldId");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_SCHEMA_VERSION_FIELDS_VERSION_FIELD",
                table: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS",
                columns: new[] { "SchemaVersionId", "DocumentFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOC_SCHEMA_VERSIONS_TYPE_STATUS",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                columns: new[] { "DocumentTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_ONE_DRAFT",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                column: "DocumentTypeId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_SCHEMA_VERSIONS_TYPE_NUMBER",
                table: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                columns: new[] { "DocumentTypeId", "VersionNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_TYPE_SCHEMA_VERSIONS_SchemaVersionId",
                table: "DOCUMENTS",
                column: "SchemaVersionId",
                principalTable: "DOCUMENT_TYPE_SCHEMA_VERSIONS",
                principalColumn: "SchemaVersionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENTS_DOCUMENT_TYPE_SCHEMA_VERSIONS_SchemaVersionId",
                table: "DOCUMENTS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_FIELD_VALUES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_FIELDS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_TYPE_SCHEMA_VERSIONS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENTS_SCHEMA_VERSION",
                table: "DOCUMENTS");

            migrationBuilder.DropColumn(
                name: "SchemaVersionId",
                table: "DOCUMENTS");
        }
    }
}
