using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.ViewModels;

public sealed partial class FirstRunWizardViewModel : ObservableObject
{
    private readonly IAppConfigRepository _repo;
    private readonly IPrerequisiteChecker _prerequisites;

    public event EventHandler? Completed;

    // ── Step tracking ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    private int _currentStep = 1;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;

    // ── Step 1 — Prerequisite status ─────────────────────────────────────────

    [ObservableProperty]
    private string _prerequisiteStatus = "Checking prerequisites…";

    // ── Step 2 — Paths ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFinish))]
    [NotifyCanExecuteChangedFor(nameof(FinishCommand))]
    private string _watchedRootPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFinish))]
    [NotifyCanExecuteChangedFor(nameof(FinishCommand))]
    private string _outputPath = string.Empty;

    public bool CanFinish =>
        !string.IsNullOrEmpty(WatchedRootPath) && Directory.Exists(WatchedRootPath) &&
        !string.IsNullOrEmpty(OutputPath) && Directory.Exists(OutputPath);

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Next() => CurrentStep++;

    [RelayCommand]
    private void BrowseRootPath()
    {
        var path = BrowseFolder();
        if (path is not null) WatchedRootPath = path;
    }

    [RelayCommand]
    private void BrowseOutputPath()
    {
        var path = BrowseFolder();
        if (path is not null) OutputPath = path;
    }

    [RelayCommand(CanExecute = nameof(CanFinish))]
    private void Finish()
    {
        _repo.Save(new AppConfig
        {
            WatchedRootPath = WatchedRootPath,
            OutputPath = OutputPath,
        });
        Completed?.Invoke(this, EventArgs.Empty);
    }

    // ── Construction ──────────────────────────────────────────────────────────

    public FirstRunWizardViewModel(IAppConfigRepository repo, IPrerequisiteChecker prerequisites)
    {
        _repo = repo;
        _prerequisites = prerequisites;
        RunPrerequisiteCheck();
    }

    private void RunPrerequisiteCheck()
    {
        Task.Run(() =>
        {
            var report = _prerequisites.CheckAll();
            PrerequisiteStatus = report switch
            {
                { PythonFound: false } => "Python was not found on PATH. Install Python 3.8+ and restart.",
                { PyMuPdfInstalled: false } =>
                    $"Python found at {report.PythonPath}. pymupdf install failed: {report.PyMuPdfInstallError}",
                _ => $"Ready — Python at {report.PythonPath}, pymupdf installed.",
            };
        });
    }

    private static string? BrowseFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Select folder" };
        return dlg.ShowDialog() == true ? dlg.FolderName : null;
    }
}
