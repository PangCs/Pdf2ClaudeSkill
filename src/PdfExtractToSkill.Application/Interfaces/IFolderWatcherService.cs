namespace PdfExtractToSkill.Application.Interfaces;

public interface IFolderWatcherService
{
    void Start(string path);
    void Stop();
    event EventHandler<string> PdfDetected;
}
