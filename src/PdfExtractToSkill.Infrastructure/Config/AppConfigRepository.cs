using System.Text.Json;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Config;

public sealed class AppConfigRepository : IAppConfigRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _configPath;
    private AppConfig? _cached;

    public AppConfigRepository()
        : this(DefaultConfigPath()) { }

    internal AppConfigRepository(string configPath)
    {
        _configPath = configPath;
    }

    public AppConfig Load()
    {
        if (_cached is not null) return _cached;
        if (!File.Exists(_configPath))
            return _cached = new AppConfig();
        var json = File.ReadAllText(_configPath);
        return _cached = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        File.WriteAllText(_configPath, JsonSerializer.Serialize(config, JsonOptions));
        _cached = config;
    }

    private static string DefaultConfigPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pdf2ClaudeSkill",
            "Setting",
            "config.json");
}
