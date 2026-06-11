using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.Infrastructure.Config;
using PdfExtractToSkill.Infrastructure.Logging;
using PdfExtractToSkill.Infrastructure.Notifications;
using PdfExtractToSkill.Infrastructure.Orchestration;
using PdfExtractToSkill.Infrastructure.Python;
using PdfExtractToSkill.Infrastructure.Shell;
using PdfExtractToSkill.Infrastructure.Skill;
using PdfExtractToSkill.Infrastructure.Watcher;

namespace PdfExtractToSkill;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private SingleInstanceGuard? _instanceGuard;
    private Views.ActivityLogWindow? _logWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceGuard = new SingleInstanceGuard();
        if (!_instanceGuard.IsOwner)
        {
            var args = string.Join(" ", e.Args);
            if (!string.IsNullOrEmpty(args))
                _instanceGuard.ForwardActivation(args);
            _instanceGuard.Dispose();
            Shutdown();
            return;
        }

        _instanceGuard.Activated += OnUriActivated;
        _services = BuildServices();

        var repo = _services.GetRequiredService<IAppConfigRepository>();
        var config = repo.Load();
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        var installState = InstallStateDetector.Detect(config.LastKnownVersion, currentVersion);

        var needsFirstRun = e.Args.Contains("--first-run") || string.IsNullOrEmpty(config.WatchedRootPath);
        if (needsFirstRun)
        {
            var vm = new ViewModels.FirstRunWizardViewModel(
                _services.GetRequiredService<IAppConfigRepository>(),
                _services.GetRequiredService<IPrerequisiteChecker>());
            var wizard = new Views.FirstRunWizard(vm);
            if (wizard.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
            // Refresh config after wizard saves it
            config = repo.Load();
            installState = InstallState.Current;
        }

        // Save current version so next launch can detect upgrades
        if (installState != InstallState.Current)
        {
            config.LastKnownVersion = currentVersion;
            repo.Save(config);
        }

        if (installState == InstallState.Upgrade)
            RunPostUpgradeCheck(currentVersion);

        _trayIcon = CreateTrayIcon();
        StartWatcher();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.GetService<IFolderWatcherService>()?.Stop();
        _trayIcon?.Dispose();
        _services?.Dispose();
        _instanceGuard?.Dispose();
        base.OnExit(e);
    }

    internal static ServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IActivityLog, ActivityLog>();
        sc.AddSingleton<IAppConfigRepository, AppConfigRepository>();
        sc.AddSingleton<IStartupRegistrar, StartupRegistrar>();
        sc.AddSingleton<IPythonDetector, PythonDetector>();
        sc.AddSingleton<IPythonRunner, PythonRunner>();
        sc.AddSingleton<IPrerequisiteChecker, PrerequisiteChecker>();
        sc.AddSingleton<ISkillNameDeriver, SkillNameDeriver>();
        sc.AddSingleton<ISkillInstaller, SkillInstaller>();
        sc.AddSingleton<IFolderWatcherService, FolderWatcherService>();
        sc.AddSingleton<INotificationService, NotificationService>();
        sc.AddSingleton<IExtractionOrchestrator, ExtractionOrchestrator>();
        sc.AddSingleton<IUninstallHelper, UninstallHelper>();
        return sc.BuildServiceProvider();
    }

    private System.Windows.Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open Settings", null, OnOpenSettings);
        menu.Items.Add("View Log", null, OnViewLog);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Prepare to Uninstall…", null, OnPrepareUninstall);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Shutdown());

        return new System.Windows.Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "PDF Extract to Skill",
            ContextMenuStrip = menu,
        };
    }

    private void StartWatcher()
    {
        var config = _services!.GetRequiredService<IAppConfigRepository>().Load();
        var watcher = _services!.GetRequiredService<IFolderWatcherService>();
        watcher.PdfDetected += OnPdfDetected;
        if (!string.IsNullOrEmpty(config.WatchedRootPath))
            watcher.Start(config.WatchedRootPath);
    }

    private void OnPdfDetected(object? sender, string pdfPath)
    {
        Task.Run(() =>
        {
            var config = _services!.GetRequiredService<IAppConfigRepository>().Load();
            var log = _services!.GetRequiredService<IActivityLog>();

            var pythonPath = config.PythonExePath
                ?? _services!.GetRequiredService<IPythonDetector>().TryDetect();

            if (pythonPath is null)
            {
                log.Append("Extraction skipped: Python not found on PATH. Set a Python path in Settings.");
                _services!.GetRequiredService<INotificationService>().ShowPythonMissing();
                return;
            }

            var fileName = Path.GetFileName(pdfPath);
            var skillName = _services!.GetRequiredService<ISkillNameDeriver>().DeriveFrom(fileName);

            var request = new ExtractionRequest(
                PdfPath: pdfPath,
                OutputPath: config.OutputPath,
                SkillName: skillName,
                Description: Path.GetFileNameWithoutExtension(fileName),
                PythonExePath: pythonPath,
                Overwrite: false);

            var result = _services!.GetRequiredService<IExtractionOrchestrator>().Extract(request);

            Dispatcher.Invoke(() =>
            {
                if (result.Success)
                    _services!.GetRequiredService<INotificationService>().ShowComplete(result.SkillName);
                else
                    _services!.GetRequiredService<INotificationService>().ShowError(result.ErrorMessage ?? "Unknown error");
            });
        });
    }

    private void OnViewLog(object? sender, EventArgs e)
    {
        if (_logWindow is not null)
        {
            _logWindow.Activate();
            return;
        }

        var vm = new ViewModels.ActivityLogViewModel(
            _services!.GetRequiredService<IActivityLog>(),
            _services!.GetRequiredService<IAppConfigRepository>());
        _logWindow = new Views.ActivityLogWindow(vm);
        _logWindow.Closed += (_, _) => _logWindow = null;
        _logWindow.Show();
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        var vm = new ViewModels.SettingsViewModel(
            _services!.GetRequiredService<IAppConfigRepository>(),
            _services!.GetRequiredService<IStartupRegistrar>(),
            _services!.GetRequiredService<IPrerequisiteChecker>());
        new Views.SettingsDialog(vm).ShowDialog();
    }

    private void OnPrepareUninstall(object? sender, EventArgs e)
    {
        var vm = new ViewModels.UninstallCleanupViewModel(
            _services!.GetRequiredService<IUninstallHelper>());
        new Views.UninstallCleanupDialog(vm).ShowDialog();
    }

    private void RunPostUpgradeCheck(string newVersion)
    {
        var prereqs = _services!.GetRequiredService<IPrerequisiteChecker>();
        var notifications = _services!.GetRequiredService<INotificationService>();
        Task.Run(() =>
        {
            var report = prereqs.CheckAll();
            Dispatcher.Invoke(() =>
            {
                if (!report.PythonFound)
                    notifications.ShowPythonMissing();
                else
                    notifications.ShowUpgraded(newVersion);
            });
        });
    }

    private void OnUriActivated(object? sender, string uri)
    {
        // TODO (N1): route pdfextracttoskill:// URI actions to the appropriate handler
        Dispatcher.Invoke(() => { /* dispatch to handler based on uri */ });
    }
}
