using Microsoft.Toolkit.Uwp.Notifications;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private const string UriScheme = "pdfextracttoskill";

    public void ShowNewPdf(string fileName, string filePath)
    {
        new ToastContentBuilder()
            .AddText("New PDF detected")
            .AddText(fileName)
            .AddButton(new ToastButton()
                .SetContent("Extract Now")
                .SetProtocolActivation(new Uri($"{UriScheme}://extract?file={Uri.EscapeDataString(filePath)}")))
            .AddButton(new ToastButtonDismiss("Dismiss"))
            .Show();
    }

    public void ShowComplete(string skillName)
    {
        new ToastContentBuilder()
            .AddText("Extraction complete")
            .AddText($"Skill \"{skillName}\" installed successfully.")
            .Show();
    }

    public void ShowError(string message)
    {
        new ToastContentBuilder()
            .AddText("Extraction failed")
            .AddText(message)
            .Show();
    }
}
