namespace PdfExtractToSkill.Application.Interfaces;

public interface INotificationService
{
    void ShowNewPdf(string fileName, string filePath);
    void ShowComplete(string skillName);
    void ShowError(string message);
}
