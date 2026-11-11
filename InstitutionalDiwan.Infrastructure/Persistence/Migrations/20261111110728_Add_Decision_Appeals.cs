using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Decision_Appeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DECISION_APPEALS",
                columns: table => new
                {
                    AppealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppealDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppellantDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OutcomeId = table.Column<int>(type: "int", nullable: false),
                    OutcomeDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    OutcomeRecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutcomeRecordedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DECISION_APPEALS", x => x.AppealId);
                    table.ForeignKey(
                        name: "FK_DECISION_APPEALS_DEPARTMENTS_AppellantDepartmentId",
                        column: x => x.AppellantDepartmentId,
                        principalTable: "DEPARTMENTS",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DECISION_APPEALS_DOCUMENT_DECISIONS_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "DOCUMENT_DECISIONS",
                        principalColumn: "DecisionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DECISION_APPEALS_AppellantDepartmentId",
                table: "DECISION_APPEALS",
                column: "AppellantDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DECISION_APPEALS_DECISION",
                table: "DECISION_APPEALS",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "UX_DECISION_APPEALS_ONE_PENDING",
                table: "DECISION_APPEALS",
                column: "DecisionId",
                unique: true,
                filter: "[OutcomeId] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DECISION_APPEALS");
        }
    }
}
