namespace PdfExtractToSkill.Application.Interfaces;

public interface IPdfQueue
{
    void Enqueue(string path);
    string? TryDequeue();
    int Count { get; }
}
