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

    public void ShowUpgraded(string newVersion)
    {
        new ToastContentBuilder()
            .AddText($"PDF Extract to Skill updated to v{newVersion}")
            .AddText("Your settings and watched folder are unchanged.")
            .Show();
    }

    public void ShowPythonMissing()
    {
        new ToastContentBuilder()
            .AddText("Python not found")
            .AddText("PDF extraction requires Python 3.8+. Install it and add it to your PATH, then restart the app.")
            .Show();
    }
}
