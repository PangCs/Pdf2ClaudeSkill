using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.ViewModels;

namespace PdfExtractToSkill.Infrastructure.Tests.ViewModels;

public class SettingsViewModelTests
{
    // ── Stubs ────────────────────────────────────────────────────────────────

    private sealed class StubRepo(AppConfig config) : IAppConfigRepository
    {
        public AppConfig? Saved { get; private set; }
        public AppConfig Load() => config;
        public void Save(AppConfig c) => Saved = c;
    }

    private sealed class StubStartup : IStartupRegistrar
    {
        public string? RegisteredPath { get; private set; }
        public bool Unregistered { get; private set; }
        public void Register(string p) => RegisteredPath = p;
        public void Unregister() => Unregistered = true;
        public bool IsRegistered() => RegisteredPath is not null;
    }

    private sealed class StubPrereqs(PrerequisiteReport report) : IPrerequisiteChecker
    {
        public PrerequisiteReport CheckAll() => report;
    }

    private static SettingsViewModel Make(AppConfig? config = null)
    {
        config ??= new AppConfig();
        return new SettingsViewModel(
            new StubRepo(config),
            new StubStartup(),
            new StubPrereqs(new PrerequisiteReport(true, @"C:\py\python.exe", true, null)));
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_LoadsConfigIntoProperties()
    {
        var vm = Make(new AppConfig
        {
            WatchedRootPath = @"C:\Watch",
            OutputPath = @"C:\Out",
            PythonExePath = @"C:\py\python.exe",
            AutostartEnabled = true,
        });

        Assert.Equal(@"C:\Watch", vm.WatchedRootPath);
        Assert.Equal(@"C:\Out", vm.OutputPath);
        Assert.Equal(@"C:\py\python.exe", vm.PythonExePath);
        Assert.True(vm.AutostartEnabled);
    }

    [Fact]
    public void IsValid_FalseWhenPathsEmpty()
    {
        var vm = Make();
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void IsValid_FalseWhenPathsDoNotExist()
    {
        var vm = Make();
        vm.WatchedRootPath = @"C:\DoesNotExist12345";
        vm.OutputPath = @"C:\DoesNotExist12345";

        Assert.False(vm.IsValid);
    }

    [Fact]
    public void IsValid_TrueWhenBothPathsExist()
    {
        var tempRoot = Path.GetTempPath();
        var vm = Make();
        vm.WatchedRootPath = tempRoot;
        vm.OutputPath = tempRoot;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void SaveCommand_DisabledWhenNotValid()
    {
        var vm = Make();
        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveCommand_EnabledWhenValid()
    {
        var temp = Path.GetTempPath();
        var vm = Make();
        vm.WatchedRootPath = temp;
        vm.OutputPath = temp;

        Assert.True(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Save_PersistsConfig()
    {
        var temp = Path.GetTempPath();
        var repo = new StubRepo(new AppConfig());
        var vm = new SettingsViewModel(repo, new StubStartup(),
            new StubPrereqs(new PrerequisiteReport(true, null, false, null)));
        vm.WatchedRootPath = temp;
        vm.OutputPath = temp;

        vm.SaveCommand.Execute(null);

        Assert.Equal(temp, repo.Saved?.WatchedRootPath);
    }

    [Fact]
    public void Save_RegistersStartupWhenAutostartEnabled()
    {
        var temp = Path.GetTempPath();
        var startup = new StubStartup();
        var vm = new SettingsViewModel(
            new StubRepo(new AppConfig()),
            startup,
            new StubPrereqs(new PrerequisiteReport(true, null, false, null)));
        vm.WatchedRootPath = temp;
        vm.OutputPath = temp;
        vm.AutostartEnabled = true;

        vm.SaveCommand.Execute(null);

        Assert.NotNull(startup.RegisteredPath);
    }

    [Fact]
    public void Save_UnregistersStartupWhenAutostartDisabled()
    {
        var temp = Path.GetTempPath();
        var startup = new StubStartup();
        var vm = new SettingsViewModel(
            new StubRepo(new AppConfig()),
            startup,
            new StubPrereqs(new PrerequisiteReport(true, null, false, null)));
        vm.WatchedRootPath = temp;
        vm.OutputPath = temp;
        vm.AutostartEnabled = false;

        vm.SaveCommand.Execute(null);

        Assert.True(startup.Unregistered);
    }

    [Fact]
    public void Save_FiresCloseRequested()
    {
        var temp = Path.GetTempPath();
        var vm = Make();
        vm.WatchedRootPath = temp;
        vm.OutputPath = temp;
        var closed = false;
        vm.CloseRequested += (_, _) => closed = true;

        vm.SaveCommand.Execute(null);

        Assert.True(closed);
    }

    [Fact]
    public void CheckPrerequisites_UpdatesStatus()
    {
        var vm = Make();
        vm.CheckPrerequisitesCommand.Execute(null);

        Assert.NotEmpty(vm.PrerequisiteStatus);
        Assert.Contains("OK", vm.PrerequisiteStatus);
    }

    [Fact]
    public void CheckPrerequisites_ReportsPythonNotFound()
    {
        var vm = new SettingsViewModel(
            new StubRepo(new AppConfig()),
            new StubStartup(),
            new StubPrereqs(new PrerequisiteReport(false, null, false, null)));
        vm.CheckPrerequisitesCommand.Execute(null);

        Assert.Contains("not found", vm.PrerequisiteStatus);
    }
}
