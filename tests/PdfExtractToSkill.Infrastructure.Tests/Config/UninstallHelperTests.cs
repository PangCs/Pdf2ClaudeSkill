using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.Infrastructure.Config;

namespace PdfExtractToSkill.Infrastructure.Tests.Config;

public class UninstallHelperTests : IDisposable
{
    private sealed class SpyRegistrar : IStartupRegistrar
    {
        public bool UnregisterCalled { get; private set; }
        public void Register(string exePath) { }
        public void Unregister() => UnregisterCalled = true;
        public bool IsRegistered() => false;
    }

    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), $"uninstall-test-{Guid.NewGuid()}");

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    [Fact]
    public void RemoveAutostart_CallsUnregisterOnRegistrar()
    {
        var spy = new SpyRegistrar();
        var helper = new UninstallHelper(spy, _tempFolder);

        helper.RemoveAutostart();

        Assert.True(spy.UnregisterCalled);
    }

    [Fact]
    public void DeleteConfigFolder_DeletesFolderWhenExists()
    {
        Directory.CreateDirectory(_tempFolder);
        File.WriteAllText(Path.Combine(_tempFolder, "config.json"), "{}");
        var helper = new UninstallHelper(new SpyRegistrar(), _tempFolder);

        helper.DeleteConfigFolder();

        Assert.False(Directory.Exists(_tempFolder));
    }

    [Fact]
    public void DeleteConfigFolder_NoopWhenFolderAbsent()
    {
        var helper = new UninstallHelper(new SpyRegistrar(), _tempFolder);

        var ex = Record.Exception(() => helper.DeleteConfigFolder());

        Assert.Null(ex);
    }

    [Fact]
    public void ConfigFolderPath_ReturnsInjectedPath()
    {
        var helper = new UninstallHelper(new SpyRegistrar(), _tempFolder);

        Assert.Equal(_tempFolder, helper.ConfigFolderPath);
    }
}
