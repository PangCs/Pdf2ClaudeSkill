using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAppConfigRepository _repo;
    private readonly IStartupRegistrar _startup;
    private readonly IPrerequisiteChecker _prerequisites;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _watchedRootPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string? _pythonExePath;

    [ObservableProperty]
    private bool _autostartEnabled;

    [ObservableProperty]
    private string _prerequisiteStatus = string.Empty;

    public bool IsValid =>
        !string.IsNullOrEmpty(WatchedRootPath) && Directory.Exists(WatchedRootPath) &&
        !string.IsNullOrEmpty(OutputPath) && Directory.Exists(OutputPath);

    public event EventHandler? CloseRequested;

    public SettingsViewModel(
        IAppConfigRepository repo,
        IStartupRegistrar startup,
        IPrerequisiteChecker prerequisites)
    {
        _repo = repo;
        _startup = startup;
        _prerequisites = prerequisites;

        var config = repo.Load();
        WatchedRootPath = config.WatchedRootPath;
        OutputPath = config.OutputPath;
        PythonExePath = config.PythonExePath;
        AutostartEnabled = config.AutostartEnabled;
    }

    private bool CanSave() => IsValid;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        _repo.Save(new AppConfig
        {
            WatchedRootPath = WatchedRootPath,
            OutputPath = OutputPath,
            PythonExePath = PythonExePath,
            AutostartEnabled = AutostartEnabled,
        });

        var exePath = Environment.ProcessPath ?? string.Empty;
        if (AutostartEnabled)
            _startup.Register(exePath);
        else
            _startup.Unregister();

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void BrowseRootPath()
    {
        var path = BrowseFolder();
        if (path is not null)
            WatchedRootPath = path;
    }

    [RelayCommand]
    private void BrowseOutputPath()
    {
        var path = BrowseFolder();
        if (path is not null)
            OutputPath = path;
    }

    [RelayCommand]
    private void BrowsePython()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Python executable (python.exe)|python.exe|All files (*.*)|*.*",
            Title = "Select Python executable",
        };
        if (dlg.ShowDialog() == true)
            PythonExePath = dlg.FileName;
    }

    [RelayCommand]
    private void CheckPrerequisites()
    {
        PrerequisiteStatus = "Checking…";
        var report = _prerequisites.CheckAll();
        PrerequisiteStatus = report switch
        {
            { PythonFound: false } => "Python not found on PATH.",
            { PyMuPdfInstalled: false } =>
                $"Python found at {report.PythonPath}. pymupdf install failed: {report.PyMuPdfInstallError}",
            _ => $"OK — Python at {report.PythonPath}, pymupdf installed.",
        };
    }

    private static string? BrowseFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Select folder" };
        return dlg.ShowDialog() == true ? dlg.FolderName : null;
    }
}
