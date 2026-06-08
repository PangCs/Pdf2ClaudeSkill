using System.Drawing;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.Infrastructure.Config;
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
        if (!string.IsNullOrEmpty(config.WatchedRootPath))
            watcher.Start(config.WatchedRootPath);
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
