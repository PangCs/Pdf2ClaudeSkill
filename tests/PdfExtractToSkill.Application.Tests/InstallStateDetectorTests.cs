using PdfExtractToSkill.Application;

namespace PdfExtractToSkill.Application.Tests;

public class InstallStateDetectorTests
{
    [Fact]
    public void FreshInstall_WhenStoredVersionIsNull()
    {
        Assert.Equal(InstallState.FreshInstall, InstallStateDetector.Detect(null, "1.0.0"));
    }

    [Fact]
    public void FreshInstall_WhenStoredVersionIsEmpty()
    {
        Assert.Equal(InstallState.FreshInstall, InstallStateDetector.Detect(string.Empty, "1.0.0"));
    }

    [Fact]
    public void Upgrade_WhenStoredVersionDiffers()
    {
        Assert.Equal(InstallState.Upgrade, InstallStateDetector.Detect("1.0.0", "1.1.0"));
    }

    [Fact]
    public void Current_WhenVersionsMatch()
    {
        Assert.Equal(InstallState.Current, InstallStateDetector.Detect("1.2.3", "1.2.3"));
    }
}
