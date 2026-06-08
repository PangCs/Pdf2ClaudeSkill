using Microsoft.Win32;
using PdfExtractToSkill.Infrastructure.Config;

namespace PdfExtractToSkill.Infrastructure.Tests.Config;

public class StartupRegistrarTests : IDisposable
{
    private readonly string _testKey = $@"SOFTWARE\PdfExtractToSkillTest\{Guid.NewGuid():N}";
    private readonly string _valueName = "TestStartup";

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(
            _testKey[.._testKey.LastIndexOf('\\')],
            throwOnMissingSubKey: false);
    }

    private StartupRegistrar Make() => new(_testKey, _valueName);

    [Fact]
    public void IsRegistered_WhenNotRegistered_ReturnsFalse()
    {
        Assert.False(Make().IsRegistered());
    }

    [Fact]
    public void Register_ThenIsRegistered_ReturnsTrue()
    {
        var reg = Make();
        reg.Register(@"C:\App\app.exe");

        Assert.True(reg.IsRegistered());
    }

    [Fact]
    public void Register_WritesExePathToRegistry()
    {
        Make().Register(@"C:\App\app.exe");

        using var key = Registry.CurrentUser.OpenSubKey(_testKey);
        Assert.Equal(@"C:\App\app.exe", key?.GetValue(_valueName));
    }

    [Fact]
    public void Unregister_AfterRegister_IsRegisteredReturnsFalse()
    {
        var reg = Make();
        reg.Register(@"C:\App\app.exe");
        reg.Unregister();

        Assert.False(reg.IsRegistered());
    }

    [Fact]
    public void Unregister_WhenNotRegistered_DoesNotThrow()
    {
        var exception = Record.Exception(() => Make().Unregister());
        Assert.Null(exception);
    }

    [Fact]
    public void Register_Twice_OverwritesPreviousValue()
    {
        var reg = Make();
        reg.Register(@"C:\Old\app.exe");
        reg.Register(@"C:\New\app.exe");

        using var key = Registry.CurrentUser.OpenSubKey(_testKey);
        Assert.Equal(@"C:\New\app.exe", key?.GetValue(_valueName));
    }
}
