namespace PdfExtractToSkill.Application.Interfaces;

public interface IStartupRegistrar
{
    void Register(string exePath);
    void Unregister();
    bool IsRegistered();
}
