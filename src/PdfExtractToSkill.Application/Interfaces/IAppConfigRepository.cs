namespace PdfExtractToSkill.Application.Interfaces;

public interface IAppConfigRepository
{
    AppConfig Load();
    void Save(AppConfig config);
}
