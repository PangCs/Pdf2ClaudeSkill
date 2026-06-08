using System.Drawing;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.Infrastructure.Config;
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
        sc.AddSingleton<IFolderWatcherService, NullFolderWatcherService>();
        return sc.BuildServiceProvider();
    }

    private System.Windows.Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open Settings", null, OnOpenSettings);
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
        // TODO (U2): open SettingsWindow
    }

    private void OnUriActivated(object? sender, string uri)
    {
        // TODO (N1): route pdfextracttoskill:// URI actions to the appropriate handler
        Dispatcher.Invoke(() => { /* dispatch to handler based on uri */ });
    }
}
