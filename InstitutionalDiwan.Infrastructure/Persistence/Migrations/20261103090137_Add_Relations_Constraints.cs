using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Relations_Constraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DOC_RELATIONS_TARGET",
                table: "DOCUMENT_RELATIONS",
                column: "TargetDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_RELATIONS_CreatedByUserId",
                table: "DOCUMENT_RELATIONS",
                column: "CreatedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DOC_RELATIONS_NOT_SELF",
                table: "DOCUMENT_RELATIONS",
                sql: "[SourceDocumentId] <> [TargetDocumentId]");

            migrationBuilder.CreateIndex(
                name: "IX_DOC_REL_EVENTS_SOURCE_DATE",
                table: "DOCUMENT_RELATION_EVENTS",
                columns: new[] { "SourceDocumentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DOC_REL_EVENTS_TARGET_DATE",
                table: "DOCUMENT_RELATION_EVENTS",
                columns: new[] { "TargetDocumentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_RELATION_EVENTS_ActorUserId",
                table: "DOCUMENT_RELATION_EVENTS",
                column: "ActorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENTS_SourceDocumentId",
                table: "DOCUMENT_RELATION_EVENTS",
                column: "SourceDocumentId",
                principalTable: "DOCUMENTS",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENTS_TargetDocumentId",
                table: "DOCUMENT_RELATION_EVENTS",
                column: "TargetDocumentId",
                principalTable: "DOCUMENTS",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_USERS_ActorUserId",
                table: "DOCUMENT_RELATION_EVENTS",
                column: "ActorUserId",
                principalTable: "USERS",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATIONS_DOCUMENTS_SourceDocumentId",
                table: "DOCUMENT_RELATIONS",
                column: "SourceDocumentId",
                principalTable: "DOCUMENTS",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATIONS_DOCUMENTS_TargetDocumentId",
                table: "DOCUMENT_RELATIONS",
                column: "TargetDocumentId",
                principalTable: "DOCUMENTS",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_RELATIONS_USERS_CreatedByUserId",
                table: "DOCUMENT_RELATIONS",
                column: "CreatedByUserId",
                principalTable: "USERS",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENTS_SourceDocumentId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_DOCUMENTS_TargetDocumentId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATION_EVENTS_USERS_ActorUserId",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATIONS_DOCUMENTS_SourceDocumentId",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATIONS_DOCUMENTS_TargetDocumentId",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_RELATIONS_USERS_CreatedByUserId",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropIndex(
                name: "IX_DOC_RELATIONS_TARGET",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_RELATIONS_CreatedByUserId",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DOC_RELATIONS_NOT_SELF",
                table: "DOCUMENT_RELATIONS");

            migrationBuilder.DropIndex(
                name: "IX_DOC_REL_EVENTS_SOURCE_DATE",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOC_REL_EVENTS_TARGET_DATE",
                table: "DOCUMENT_RELATION_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_RELATION_EVENTS_ActorUserId",
                table: "DOCUMENT_RELATION_EVENTS");
        }
    }
}
