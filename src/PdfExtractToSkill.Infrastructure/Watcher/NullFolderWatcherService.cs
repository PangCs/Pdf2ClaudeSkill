using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Watcher;

public sealed class NullFolderWatcherService : IFolderWatcherService
{
    public void Start(string path) { }
    public void Stop() { }
    public event EventHandler<string>? PdfDetected;
}
