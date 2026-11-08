using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Department_Register_Visits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.AddColumn<Guid>(
                name: "RegistryVisitId",
                table: "DOCUMENT_MOVEMENT_STEPS",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DOCUMENT_REGISTRY_VISITS",
                columns: table => new
                {
                    VisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistryDate = table.Column<DateTime>(type: "date", nullable: false),
                    MovementStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_REGISTRY_VISITS", x => x.VisitId);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_REGISTRY_VISITS_DOCUMENT_NUMBERS_DocumentNumberId",
                        column: x => x.DocumentNumberId,
                        principalTable: "DOCUMENT_NUMBERS",
                        principalColumn: "DocumentNumberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "DocumentId", "DepartmentId", "NumberingYear" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_MOVEMENT_STEPS_RegistryVisitId",
                table: "DOCUMENT_MOVEMENT_STEPS",
                column: "RegistryVisitId");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_REG_VISITS_NUMBER_DATE",
                table: "DOCUMENT_REGISTRY_VISITS",
                columns: new[] { "DocumentNumberId", "RegistryDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_MOVEMENT_STEPS_DOCUMENT_REGISTRY_VISITS_RegistryVisitId",
                table: "DOCUMENT_MOVEMENT_STEPS",
                column: "RegistryVisitId",
                principalTable: "DOCUMENT_REGISTRY_VISITS",
                principalColumn: "VisitId",
                onDelete: ReferentialAction.Restrict);

            // Each past registration date becomes a visit: the dates written in history (4 = registered,
            // 5 = re-registered) plus the line's current date, keeping the earliest row per date.
            migrationBuilder.Sql(@"
INSERT INTO DOCUMENT_REGISTRY_VISITS (VisitId, DocumentNumberId, RegistryDate, MovementStepId, RecordedByUserId, RecordedAt, Notes)
SELECT NEWID(), c.DocumentNumberId, c.VisitDate, NULL, c.UserId, c.RecordedAt, NULL
FROM (
    SELECT src.*, ROW_NUMBER() OVER (PARTITION BY src.DocumentNumberId, src.VisitDate ORDER BY src.RecordedAt) AS rn
    FROM (
        SELECT n.DocumentNumberId, CAST(h.EffectiveDate AS date) AS VisitDate, h.ActorUserId AS UserId, h.OccurredAt AS RecordedAt
        FROM DOCUMENT_NUMBER_HISTORY h
        JOIN DOCUMENT_NUMBERS n ON n.DocumentNumberId = h.DocumentNumberId
        WHERE n.ScopeId = 2 AND h.EventTypeId IN (4, 5) AND h.EffectiveDate IS NOT NULL
        UNION ALL
        SELECT n.DocumentNumberId, CAST(n.RegistryDate AS date), n.IssuedByUserId, n.IssuedAt
        FROM DOCUMENT_NUMBERS n
        WHERE n.ScopeId = 2 AND n.RegistryDate IS NOT NULL
    ) src
) c
WHERE c.rn = 1;");

            // Past receipts are linked to the visit dated the same day; the rest keep a null visit.
            migrationBuilder.Sql(@"
UPDATE s SET s.RegistryVisitId = v.VisitId
FROM DOCUMENT_MOVEMENT_STEPS s
JOIN DOCUMENT_REGISTRY_VISITS v
    ON v.DocumentNumberId = s.RegistryNumberId AND v.RegistryDate = CAST(s.ReceivedAt AS date)
WHERE s.RegistryNumberId IS NOT NULL AND s.ReceivedAt IS NOT NULL;

UPDATE v SET v.MovementStepId = x.StepId
FROM DOCUMENT_REGISTRY_VISITS v
CROSS APPLY (
    SELECT TOP 1 s.StepId
    FROM DOCUMENT_MOVEMENT_STEPS s
    WHERE s.RegistryVisitId = v.VisitId
    ORDER BY s.ReceivedAt
) x;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_MOVEMENT_STEPS_DOCUMENT_REGISTRY_VISITS_RegistryVisitId",
                table: "DOCUMENT_MOVEMENT_STEPS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_REGISTRY_VISITS");

            migrationBuilder.DropIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_MOVEMENT_STEPS_RegistryVisitId",
                table: "DOCUMENT_MOVEMENT_STEPS");

            migrationBuilder.DropColumn(
                name: "RegistryVisitId",
                table: "DOCUMENT_MOVEMENT_STEPS");

            migrationBuilder.CreateIndex(
                name: "UX_DOC_NUMBERS_DOC_DEPT",
                table: "DOCUMENT_NUMBERS",
                columns: new[] { "DocumentId", "DepartmentId" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL");
        }
    }
}
