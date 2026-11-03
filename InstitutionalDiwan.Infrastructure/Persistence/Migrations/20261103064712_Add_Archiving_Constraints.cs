using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Archiving_Constraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_EventTypeId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.CreateIndex(
                name: "UX_SAVED_SEARCHES_USER_DEFAULT",
                table: "SAVED_SEARCHES",
                column: "UserId",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_SAVED_SEARCHES_USER_NAME",
                table: "SAVED_SEARCHES",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOC_ARCHIVE_EVENTS_TYPE_DATE",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                columns: new[] { "EventTypeId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_ActorUserId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_NewStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_PreviousStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "PreviousStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENTS_DocumentId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "DocumentId",
                principalTable: "DOCUMENTS",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENT_STATUS_TYPES_NewStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "NewStatusId",
                principalTable: "DOCUMENT_STATUS_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENT_STATUS_TYPES_PreviousStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "PreviousStatusId",
                principalTable: "DOCUMENT_STATUS_TYPES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_USERS_ActorUserId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "ActorUserId",
                principalTable: "USERS",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SAVED_SEARCHES_USERS_UserId",
                table: "SAVED_SEARCHES",
                column: "UserId",
                principalTable: "USERS",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENTS_DocumentId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENT_STATUS_TYPES_NewStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_DOCUMENT_STATUS_TYPES_PreviousStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCUMENT_ARCHIVE_EVENTS_USERS_ActorUserId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_SAVED_SEARCHES_USERS_UserId",
                table: "SAVED_SEARCHES");

            migrationBuilder.DropIndex(
                name: "UX_SAVED_SEARCHES_USER_DEFAULT",
                table: "SAVED_SEARCHES");

            migrationBuilder.DropIndex(
                name: "UX_SAVED_SEARCHES_USER_NAME",
                table: "SAVED_SEARCHES");

            migrationBuilder.DropIndex(
                name: "IX_DOC_ARCHIVE_EVENTS_TYPE_DATE",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_ActorUserId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_NewStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.DropIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_PreviousStatusId",
                table: "DOCUMENT_ARCHIVE_EVENTS");

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_ARCHIVE_EVENTS_EventTypeId",
                table: "DOCUMENT_ARCHIVE_EVENTS",
                column: "EventTypeId");
        }
    }
}
