namespace warlinghamcc.PdfService.Models;

public class GeneratePdfRequest
{
    public string Title { get; set; } = "Newsletter";
    public string BaseUrl { get; set; } = "https://warlinghamcc.co.uk";
    public string Html { get; set; } = string.Empty;
}
