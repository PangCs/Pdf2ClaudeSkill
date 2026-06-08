namespace PdfExtractToSkill.Application.Interfaces;

public interface IExtractionOrchestrator
{
    ExtractionResult Extract(ExtractionRequest request);
}
