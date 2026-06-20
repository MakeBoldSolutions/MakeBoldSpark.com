using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MakeBoldSpark.Recipe.Data;

public class RecipeDbContextFactory : IDesignTimeDbContextFactory<RecipeDbContext>
{
    public RecipeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RecipeDbContext>();
        optionsBuilder.UseSqlite("Data Source=makeboldspark-recipe-design.db",
            b => b.MigrationsAssembly("MakeBoldSpark.Recipe"));
        return new RecipeDbContext(optionsBuilder.Options);
    }
}
