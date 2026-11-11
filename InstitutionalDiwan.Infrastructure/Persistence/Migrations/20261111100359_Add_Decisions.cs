using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Decisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DECISION_TYPES",
                columns: table => new
                {
                    DecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsAppealable = table.Column<bool>(type: "bit", nullable: false),
                    EffectId = table.Column<int>(type: "int", nullable: false),
                    DestinationDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DECISION_TYPES", x => x.DecisionTypeId);
                    table.ForeignKey(
                        name: "FK_DECISION_TYPES_DEPARTMENTS_DestinationDepartmentId",
                        column: x => x.DestinationDepartmentId,
                        principalTable: "DEPARTMENTS",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DECISION_TYPE_DOCUMENT_TYPES",
                columns: table => new
                {
                    DecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DECISION_TYPE_DOCUMENT_TYPES", x => new { x.DecisionTypeId, x.DocumentTypeId });
                    table.ForeignKey(
                        name: "FK_DECISION_TYPE_DOCUMENT_TYPES_DECISION_TYPES_DecisionTypeId",
                        column: x => x.DecisionTypeId,
                        principalTable: "DECISION_TYPES",
                        principalColumn: "DecisionTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DECISION_TYPE_DOCUMENT_TYPES_DOCUMENT_TYPES_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DOCUMENT_TYPES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_DECISIONS",
                columns: table => new
                {
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DecisionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DecisionYear = table.Column<int>(type: "int", nullable: false),
                    IssuingDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistryVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    IsAppealable = table.Column<bool>(type: "bit", nullable: false),
                    EffectId = table.Column<int>(type: "int", nullable: false),
                    DestinationDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StatusChangedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_DECISIONS", x => x.DecisionId);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_DECISIONS_DECISION_TYPES_DecisionTypeId",
                        column: x => x.DecisionTypeId,
                        principalTable: "DECISION_TYPES",
                        principalColumn: "DecisionTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_DECISIONS_DEPARTMENTS_DestinationDepartmentId",
                        column: x => x.DestinationDepartmentId,
                        principalTable: "DEPARTMENTS",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_DECISIONS_DEPARTMENTS_IssuingDepartmentId",
                        column: x => x.IssuingDepartmentId,
                        principalTable: "DEPARTMENTS",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_DECISIONS_DOCUMENTS_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "DOCUMENTS",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_DECISIONS_DOCUMENT_FILES_DocumentFileId",
                        column: x => x.DocumentFileId,
                        principalTable: "DOCUMENT_FILES",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_DECISIONS_DOCUMENT_REGISTRY_VISITS_RegistryVisitId",
                        column: x => x.RegistryVisitId,
                        principalTable: "DOCUMENT_REGISTRY_VISITS",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DECISION_TYPE_DOCUMENT_TYPES_DocumentTypeId",
                table: "DECISION_TYPE_DOCUMENT_TYPES",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DECISION_TYPES_DestinationDepartmentId",
                table: "DECISION_TYPES",
                column: "DestinationDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_DECISIONS_DecisionTypeId",
                table: "DOCUMENT_DECISIONS",
                column: "DecisionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_DECISIONS_DestinationDepartmentId",
                table: "DOCUMENT_DECISIONS",
                column: "DestinationDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_DECISIONS_DocumentFileId",
                table: "DOCUMENT_DECISIONS",
                column: "DocumentFileId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_DECISIONS_RegistryVisitId",
                table: "DOCUMENT_DECISIONS",
                column: "RegistryVisitId");

            migrationBuilder.CreateIndex(
                name: "UX_DOCUMENT_DECISIONS_DEPT_YEAR_NUMBER",
                table: "DOCUMENT_DECISIONS",
                columns: new[] { "IssuingDepartmentId", "DecisionYear", "DecisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DOCUMENT_DECISIONS_ONE_ACTIVE",
                table: "DOCUMENT_DECISIONS",
                column: "DocumentId",
                unique: true,
                filter: "[StatusId] IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DECISION_TYPE_DOCUMENT_TYPES");

            migrationBuilder.DropTable(
                name: "DOCUMENT_DECISIONS");

            migrationBuilder.DropTable(
                name: "DECISION_TYPES");
        }
    }
}
