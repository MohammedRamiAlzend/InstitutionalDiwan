using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Notifications_Constraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_NotificationId",
                table: "NOTIFICATION_RECIPIENTS");

            migrationBuilder.AlterColumn<string>(
                name: "PayloadJson",
                table: "NOTIFICATION_OUTBOX",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_CREATED",
                table: "NOTIFICATIONS",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_RelatedDocumentId",
                table: "NOTIFICATIONS",
                column: "RelatedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_RelatedMovementId",
                table: "NOTIFICATIONS",
                column: "RelatedMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_DELIVERED",
                table: "NOTIFICATION_RECIPIENTS",
                column: "DeliveredAt");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_DepartmentId",
                table: "NOTIFICATION_RECIPIENTS",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_INBOX",
                table: "NOTIFICATION_RECIPIENTS",
                columns: new[] { "UserId", "IsDismissed", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "UX_NOTIFICATION_RECIPIENTS_NOTIFICATION_USER",
                table: "NOTIFICATION_RECIPIENTS",
                columns: new[] { "NotificationId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_RECIPIENTS_DEPARTMENTS_DepartmentId",
                table: "NOTIFICATION_RECIPIENTS",
                column: "DepartmentId",
                principalTable: "DEPARTMENTS",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_RECIPIENTS_USERS_UserId",
                table: "NOTIFICATION_RECIPIENTS",
                column: "UserId",
                principalTable: "USERS",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATIONS_DOCUMENTS_RelatedDocumentId",
                table: "NOTIFICATIONS",
                column: "RelatedDocumentId",
                principalTable: "DOCUMENTS",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATIONS_DOCUMENT_MOVEMENTS_RelatedMovementId",
                table: "NOTIFICATIONS",
                column: "RelatedMovementId",
                principalTable: "DOCUMENT_MOVEMENTS",
                principalColumn: "MovementId",
                onDelete: ReferentialAction.Restrict);

            // Only IN_APP is delivered; the seed no longer lists EMAIL for registration notices.
            migrationBuilder.Sql("UPDATE NOTIFICATION_TYPES SET DefaultChannelsCsv = 'IN_APP' WHERE Id = 2 AND DefaultChannelsCsv = 'IN_APP,EMAIL'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_RECIPIENTS_DEPARTMENTS_DepartmentId",
                table: "NOTIFICATION_RECIPIENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_RECIPIENTS_USERS_UserId",
                table: "NOTIFICATION_RECIPIENTS");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATIONS_DOCUMENTS_RelatedDocumentId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATIONS_DOCUMENT_MOVEMENTS_RelatedMovementId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATIONS_CREATED",
                table: "NOTIFICATIONS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATIONS_RelatedDocumentId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATIONS_RelatedMovementId",
                table: "NOTIFICATIONS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_DELIVERED",
                table: "NOTIFICATION_RECIPIENTS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_DepartmentId",
                table: "NOTIFICATION_RECIPIENTS");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_INBOX",
                table: "NOTIFICATION_RECIPIENTS");

            migrationBuilder.DropIndex(
                name: "UX_NOTIFICATION_RECIPIENTS_NOTIFICATION_USER",
                table: "NOTIFICATION_RECIPIENTS");

            migrationBuilder.AlterColumn<string>(
                name: "PayloadJson",
                table: "NOTIFICATION_OUTBOX",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_RECIPIENTS_NotificationId",
                table: "NOTIFICATION_RECIPIENTS",
                column: "NotificationId");
        }
    }
}
