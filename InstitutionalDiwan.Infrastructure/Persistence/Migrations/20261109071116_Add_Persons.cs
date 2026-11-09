using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutionalDiwan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Persons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DOCUMENT_PERSON_ROLES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_PERSON_ROLES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GENDERS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GENDERS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NATIONALITIES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Desc = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NATIONALITIES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PERSONS",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MotherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NationalNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CivilRegistryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CivilRegistryPlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BirthPlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    GenderId = table.Column<int>(type: "int", nullable: true),
                    NationalityId = table.Column<int>(type: "int", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERSONS", x => x.PersonId);
                    table.ForeignKey(
                        name: "FK_PERSONS_GENDERS_GenderId",
                        column: x => x.GenderId,
                        principalTable: "GENDERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSONS_NATIONALITIES_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "NATIONALITIES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOCUMENT_PERSONS",
                columns: table => new
                {
                    DocumentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOCUMENT_PERSONS", x => x.DocumentPersonId);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_PERSONS_DOCUMENTS_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "DOCUMENTS",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_PERSONS_DOCUMENT_PERSON_ROLES_RoleId",
                        column: x => x.RoleId,
                        principalTable: "DOCUMENT_PERSON_ROLES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOCUMENT_PERSONS_PERSONS_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PERSONS",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_PERSONS_PERSON_DOC",
                table: "DOCUMENT_PERSONS",
                columns: new[] { "PersonId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DOCUMENT_PERSONS_RoleId",
                table: "DOCUMENT_PERSONS",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UX_DOCUMENT_PERSONS_DOC_PERSON",
                table: "DOCUMENT_PERSONS",
                columns: new[] { "DocumentId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PERSONS_GenderId",
                table: "PERSONS",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSONS_NationalityId",
                table: "PERSONS",
                column: "NationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSONS_NORMALIZED_NAME",
                table: "PERSONS",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "UX_PERSONS_NATIONAL_NUMBER",
                table: "PERSONS",
                column: "NationalNumber",
                unique: true,
                filter: "[NationalNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DOCUMENT_PERSONS");

            migrationBuilder.DropTable(
                name: "DOCUMENT_PERSON_ROLES");

            migrationBuilder.DropTable(
                name: "PERSONS");

            migrationBuilder.DropTable(
                name: "GENDERS");

            migrationBuilder.DropTable(
                name: "NATIONALITIES");
        }
    }
}
