namespace PdfExtractToSkill.Application.Interfaces;

public interface IUninstallHelper
{
    string ConfigFolderPath { get; }
    void RemoveAutostart();
    void DeleteConfigFolder();
}
