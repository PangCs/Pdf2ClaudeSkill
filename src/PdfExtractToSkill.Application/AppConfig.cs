namespace PdfExtractToSkill.Application;

public sealed class AppConfig
{
    public string WatchedRootPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string? PythonExePath { get; set; }
    public bool AutostartEnabled { get; set; }
    public string? LastKnownVersion { get; set; }
}
