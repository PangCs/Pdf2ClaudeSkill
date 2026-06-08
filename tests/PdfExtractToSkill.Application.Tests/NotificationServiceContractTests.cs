using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Application.Tests;

public class NotificationServiceContractTests
{
    private sealed class RecordingNotificationService : INotificationService
    {
        public readonly List<(string Method, string Arg1, string? Arg2)> Calls = [];

        public void ShowNewPdf(string fileName, string filePath) =>
            Calls.Add(("ShowNewPdf", fileName, filePath));

        public void ShowComplete(string skillName) =>
            Calls.Add(("ShowComplete", skillName, null));

        public void ShowError(string message) =>
            Calls.Add(("ShowError", message, null));

        public void ShowUpgraded(string newVersion) =>
            Calls.Add(("ShowUpgraded", newVersion, null));

        public void ShowPythonMissing() =>
            Calls.Add(("ShowPythonMissing", string.Empty, null));
    }

    [Fact]
    public void ShowNewPdf_PassesFileNameAndPath()
    {
        var svc = new RecordingNotificationService();
        svc.ShowNewPdf("doc.pdf", @"C:\Watch\doc.pdf");

        var (method, fileName, filePath) = Assert.Single(svc.Calls);
        Assert.Equal("ShowNewPdf", method);
        Assert.Equal("doc.pdf", fileName);
        Assert.Equal(@"C:\Watch\doc.pdf", filePath);
    }

    [Fact]
    public void ShowComplete_PassesSkillName()
    {
        var svc = new RecordingNotificationService();
        svc.ShowComplete("my-skill");

        var (method, skillName, _) = Assert.Single(svc.Calls);
        Assert.Equal("ShowComplete", method);
        Assert.Equal("my-skill", skillName);
    }

    [Fact]
    public void ShowError_PassesMessage()
    {
        var svc = new RecordingNotificationService();
        svc.ShowError("Something went wrong");

        var (method, message, _) = Assert.Single(svc.Calls);
        Assert.Equal("ShowError", method);
        Assert.Equal("Something went wrong", message);
    }
}
