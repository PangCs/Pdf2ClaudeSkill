using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.ViewModels;

namespace PdfExtractToSkill.Infrastructure.Tests.ViewModels;

public class FirstRunWizardViewModelTests
{
    private sealed class StubRepo : IAppConfigRepository
    {
        public AppConfig? Saved { get; private set; }
        public AppConfig Load() => new();
        public void Save(AppConfig c) => Saved = c;
    }

    private sealed class StubPrereqs(PrerequisiteReport report) : IPrerequisiteChecker
    {
        public PrerequisiteReport CheckAll() => report;
    }

    private static FirstRunWizardViewModel Make(PrerequisiteReport? report = null)
    {
        report ??= new PrerequisiteReport(true, @"C:\py\python.exe", true, null);
        return new FirstRunWizardViewModel(new StubRepo(), new StubPrereqs(report));
    }

    [Fact]
    public void Constructor_StartsAtStep1()
    {
        var vm = Make();
        Assert.Equal(1, vm.CurrentStep);
        Assert.True(vm.IsStep1);
    }

    [Fact]
    public void Next_AdvancesStep()
    {
        var vm = Make();
        vm.NextCommand.Execute(null);
        Assert.Equal(2, vm.CurrentStep);
        Assert.True(vm.IsStep2);
    }

    [Fact]
    public void CanFinish_FalseWhenPathsEmpty()
    {
        var vm = Make();
        Assert.False(vm.CanFinish);
    }

    [Fact]
    public void CanFinish_TrueWhenBothPathsExist()
    {
        var temp = System.IO.Path.GetTempPath();
        var vm = Make();
        vm.WatchedRootPath = temp;
        vm.OutputPath = temp;
        Assert.True(vm.CanFinish);
    }

    [Fact]
    public void Finish_SavesConfig()
    {
        var temp = System.IO.Path.GetTempPath();
        var repo = new StubRepo();
        var vm = new FirstRunWizardViewModel(repo,
            new StubPrereqs(new PrerequisiteReport(true, @"C:\py\python.exe", true, null)));
        vm.WatchedRootPath = temp;
        vm.OutputPath = temp;

        vm.FinishCommand.Execute(null);

        Assert.Equal(temp, repo.Saved?.WatchedRootPath);
        Assert.Equal(temp, repo.Saved?.OutputPath);
    }

    [Fact]
    public void Finish_FiresCompleted()
    {
        var temp = System.IO.Path.GetTempPath();
        var vm = Make();
        vm.WatchedRootPath = temp;
        vm.OutputPath = temp;
        var completed = false;
        vm.Completed += (_, _) => completed = true;

        vm.FinishCommand.Execute(null);

        Assert.True(completed);
    }
}
