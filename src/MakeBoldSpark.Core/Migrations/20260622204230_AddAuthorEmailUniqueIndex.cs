using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MakeBoldSpark.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorEmailUniqueIndex : Migration
    {
        // EF's auto-diff against this context's stale migration history also proposed dropping
        // the Recipe/RecipeCategory/RecipeComment/RecipeImage tables — pre-existing drift from
        // those entities moving out of MakeBoldSparkCoreDbContext's model into
        // MakeBoldSpark.Recipe's own RecipeDbContext (which, in dev, shares the same physical
        // SQLite file). That drop is unrelated to this migration's purpose and would destroy
        // live data still managed by RecipeDbContext's own migration history, so it is
        // deliberately omitted here. This migration does only what tasks.md T054a calls for:
        // enforce Author.Email uniqueness at the database level.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Authors_Email",
                table: "Authors",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Authors_Email",
                table: "Authors");
        }
    }
}
