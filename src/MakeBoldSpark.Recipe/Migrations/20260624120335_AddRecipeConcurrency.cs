using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MakeBoldSpark.Recipe.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RecipeImage",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RecipeComment",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RecipeCategory",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Recipe",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeCategory_DomainId_Name",
                table: "RecipeCategory",
                columns: new[] { "DomainId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipe_DomainId_Name",
                table: "Recipe",
                columns: new[] { "DomainId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeCategory_DomainId_Name",
                table: "RecipeCategory");

            migrationBuilder.DropIndex(
                name: "IX_Recipe_DomainId_Name",
                table: "Recipe");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RecipeImage");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RecipeComment");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RecipeCategory");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Recipe");
        }
    }
}
