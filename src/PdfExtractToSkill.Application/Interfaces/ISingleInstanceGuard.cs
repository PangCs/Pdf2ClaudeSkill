namespace PdfExtractToSkill.Application.Interfaces;

public interface ISingleInstanceGuard : IDisposable
{
    bool IsOwner { get; }
    void ForwardActivation(string args);
    event EventHandler<string>? Activated;
}
