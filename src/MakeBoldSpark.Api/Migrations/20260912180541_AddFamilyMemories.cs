using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MakeBoldSpark.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyMemories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FamilyMemories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PersonPronouns = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RelationshipToJared = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMemories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMemories_IsPublished_SortOrder_Id",
                table: "FamilyMemories",
                columns: new[] { "IsPublished", "SortOrder", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyMemories");
        }
    }
}
