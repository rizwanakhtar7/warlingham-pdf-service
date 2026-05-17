namespace warlinghamcc.PdfService.Services;

public interface IPlaywrightPdfService
{
    Task<byte[]> GeneratePdfAsync(string html, string title, string baseUrl, CancellationToken cancellationToken = default);
}
