using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Lookup_IsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "USER_SCOPE_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "STORAGE_PROVIDER_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "RELATION_ACTION_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "PARTICIPANT_TARGET_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "PARTICIPANT_ROLES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "ORGANIZATION_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "NUMBERING_RESET_POLICIES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "NUMBERING_MODES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "NOTIFICATION_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "NOTIFICATION_PRIORITIES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "NOTIFICATION_OUTBOX_STATUSES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "NOTIFICATION_CHANNELS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "MOVEMENT_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "MOVEMENT_STEP_STATUSES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "MOVEMENT_STATUSES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "MOVEMENT_EVENT_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_STATUS_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_RELATION_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_PRIORITY_LEVELS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_NUMBER_STATUSES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_NUMBER_SCOPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_NUMBER_EVENT_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_FILE_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_DIRECTIONS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DOCUMENT_CONFIDENTIALITY_LEVELS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DEPARTMENT_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "AUDIT_ENTITY_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "AUDIT_ACTION_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "ASSIGNMENT_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "ARCHIVE_LOCATIONS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "ARCHIVE_EVENT_TYPES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "ACCESS_LEVELS",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "USER_SCOPE_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "STORAGE_PROVIDER_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "RELATION_ACTION_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "PARTICIPANT_TARGET_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "PARTICIPANT_ROLES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "ORGANIZATION_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "NUMBERING_RESET_POLICIES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "NUMBERING_MODES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "NOTIFICATION_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "NOTIFICATION_PRIORITIES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "NOTIFICATION_OUTBOX_STATUSES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "NOTIFICATION_CHANNELS");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "MOVEMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "MOVEMENT_STEP_STATUSES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "MOVEMENT_STATUSES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "MOVEMENT_EVENT_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_STATUS_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_RELATION_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_PRIORITY_LEVELS");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_NUMBER_STATUSES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_NUMBER_SCOPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_NUMBER_EVENT_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_FILE_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_DIRECTIONS");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DOCUMENT_CONFIDENTIALITY_LEVELS");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DEPARTMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "AUDIT_ENTITY_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "AUDIT_ACTION_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "ASSIGNMENT_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "ARCHIVE_LOCATIONS");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "ARCHIVE_EVENT_TYPES");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "ACCESS_LEVELS");
        }
    }
}
