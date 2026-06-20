using Microsoft.EntityFrameworkCore.Design;

namespace MakeBoldSpark.Core.Data;

public class MakeBoldSparkCoreDbContextFactory : IDesignTimeDbContextFactory<MakeBoldSparkCoreDbContext>
{
    public MakeBoldSparkCoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MakeBoldSparkCoreDbContext>();
        optionsBuilder.UseSqlite("Data Source=c:\\websites\\MakeBoldSpark\\MakeBoldSpark.db");

        return new MakeBoldSparkCoreDbContext(optionsBuilder.Options);
    }
}
