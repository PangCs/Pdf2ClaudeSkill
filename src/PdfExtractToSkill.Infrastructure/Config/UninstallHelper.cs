using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Config;

public sealed class UninstallHelper : IUninstallHelper
{
    private readonly IStartupRegistrar _startupRegistrar;

    public string ConfigFolderPath { get; }

    public UninstallHelper(IStartupRegistrar startupRegistrar)
        : this(startupRegistrar, DefaultConfigFolder()) { }

    internal UninstallHelper(IStartupRegistrar startupRegistrar, string configFolderPath)
    {
        _startupRegistrar = startupRegistrar;
        ConfigFolderPath = configFolderPath;
    }

    public void RemoveAutostart() => _startupRegistrar.Unregister();

    public void DeleteConfigFolder()
    {
        if (Directory.Exists(ConfigFolderPath))
            Directory.Delete(ConfigFolderPath, recursive: true);
    }

    private static string DefaultConfigFolder() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PdfExtractToSkill");
}
