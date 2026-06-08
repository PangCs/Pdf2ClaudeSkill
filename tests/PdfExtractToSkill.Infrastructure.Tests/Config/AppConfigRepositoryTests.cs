using PdfExtractToSkill.Application;
using PdfExtractToSkill.Infrastructure.Config;

namespace PdfExtractToSkill.Infrastructure.Tests.Config;

public class AppConfigRepositoryTests : IDisposable
{
    private readonly string _configPath =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.json");

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_configPath)!;
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    private AppConfigRepository Make() => new(_configPath);

    [Fact]
    public void Load_WhenFileAbsent_ReturnsDefaultConfig()
    {
        var config = Make().Load();

        Assert.Equal(string.Empty, config.WatchedRootPath);
        Assert.Equal(string.Empty, config.OutputPath);
        Assert.Null(config.PythonExePath);
        Assert.False(config.AutostartEnabled);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsAllProperties()
    {
        var repo = Make();
        var original = new AppConfig
        {
            WatchedRootPath = @"C:\Watch",
            OutputPath = @"C:\Output",
            PythonExePath = @"C:\Python\python.exe",
            AutostartEnabled = true,
        };

        repo.Save(original);
        var loaded = repo.Load();

        Assert.Equal(original.WatchedRootPath, loaded.WatchedRootPath);
        Assert.Equal(original.OutputPath, loaded.OutputPath);
        Assert.Equal(original.PythonExePath, loaded.PythonExePath);
        Assert.Equal(original.AutostartEnabled, loaded.AutostartEnabled);
    }

    [Fact]
    public void Save_CreatesIntermediateDirectories()
    {
        Make().Save(new AppConfig { WatchedRootPath = @"C:\Watch", OutputPath = @"C:\Output" });

        Assert.True(File.Exists(_configPath));
    }

    [Fact]
    public void Save_WritesValidJson()
    {
        Make().Save(new AppConfig { WatchedRootPath = @"C:\Watch", OutputPath = @"C:\Output" });

        var text = File.ReadAllText(_configPath);
        Assert.Contains("watchedRootPath", text);
        Assert.Contains("outputPath", text);
    }

    [Fact]
    public void Save_NullPythonExePath_RoundTripsAsNull()
    {
        var repo = Make();
        repo.Save(new AppConfig { WatchedRootPath = @"C:\Watch", OutputPath = @"C:\Out", PythonExePath = null });

        Assert.Null(repo.Load().PythonExePath);
    }

    [Fact]
    public void Save_Overwrites_ExistingConfig()
    {
        var repo = Make();
        repo.Save(new AppConfig { WatchedRootPath = @"C:\Old", OutputPath = @"C:\Out" });
        repo.Save(new AppConfig { WatchedRootPath = @"C:\New", OutputPath = @"C:\Out" });

        Assert.Equal(@"C:\New", repo.Load().WatchedRootPath);
    }
}
