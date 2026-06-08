using System.Windows;

namespace PdfExtractToSkill;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // TODO (C3): initialise system tray icon and wire FolderWatcherService
    }
}
