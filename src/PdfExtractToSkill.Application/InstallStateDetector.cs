namespace PdfExtractToSkill.Application;

public enum InstallState
{
    FreshInstall,
    Upgrade,
    Current,
}

public static class InstallStateDetector
{
    public static InstallState Detect(string? storedVersion, string currentVersion)
    {
        if (string.IsNullOrEmpty(storedVersion)) return InstallState.FreshInstall;
        if (storedVersion != currentVersion) return InstallState.Upgrade;
        return InstallState.Current;
    }
}
