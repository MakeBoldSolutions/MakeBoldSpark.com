using System.Runtime.InteropServices;
using MakeBoldSpark.Core.Data;

namespace MakeBoldSpark.Core.Providers;

public interface IAboutProvider
{
    Task<Models.AboutModel> GetAboutModel();
}

public class AboutProvider : IAboutProvider
{
    private readonly MakeBoldSparkCoreDbContext _db;

    public AboutProvider(MakeBoldSparkCoreDbContext db)
    {
        _db = db;
    }
    public async Task<Models.AboutModel> GetAboutModel()
    {
        var model = new Models.AboutModel();
        model.DatabaseProvider = _db.Database?.ProviderName ?? string.Empty;
        model.OperatingSystem = RuntimeInformation.OSDescription;
        try
        {
            model.Build = new Models.BuildVersion(Assembly.GetExecutingAssembly());
            model.Version = model.Build.ToString();
        }
        catch
        {
            var attr = typeof(AboutProvider)
                .GetTypeInfo()
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            model.Version = attr?.InformationalVersion ?? string.Empty;

        }


        return await Task.FromResult(model);
    }
}
