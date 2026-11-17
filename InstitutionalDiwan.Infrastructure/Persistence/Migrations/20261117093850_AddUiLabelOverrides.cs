using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261117093850_AddUiLabelOverrides : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UI_LABEL_OVERRIDES",
            columns: table => new
            {
                LabelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UI_LABEL_OVERRIDES", x => x.LabelId);
            });

        migrationBuilder.CreateIndex(
            name: "UX_UI_LABEL_OVERRIDES_KEY",
            table: "UI_LABEL_OVERRIDES",
            column: "Key",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UI_LABEL_OVERRIDES");
    }
}
