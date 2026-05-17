using Microsoft.AspNetCore.Mvc;
using warlinghamcc.PdfService.Models;
using warlinghamcc.PdfService.Services;

namespace warlinghamcc.PdfService.Controllers;

[ApiController]
[Route("api/pdf")]
public class PdfController : ControllerBase
{
    private readonly IPlaywrightPdfService _pdfService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PdfController> _logger;

    public PdfController(
        IPlaywrightPdfService pdfService,
        IConfiguration configuration,
        ILogger<PdfController> logger)
    {
        _pdfService = pdfService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("generate")]
    [RequestSizeLimit(1_000_000)]
    public async Task<IActionResult> Generate([FromBody] GeneratePdfRequest request, CancellationToken cancellationToken)
    {
        try
        {
            //var authResult = ValidateApiKey();
            //if (authResult != null)
            //{
            //    return authResult;
            //}

            if (request == null || string.IsNullOrWhiteSpace(request.Html))
            {
                return BadRequest("HTML is required.");
            }

            var maxHtmlLength = _configuration.GetValue<int?>("Pdf:MaxHtmlLength") ?? 500_000;
            if (request.Html.Length > maxHtmlLength)
            {
                return BadRequest($"HTML is too large. Max allowed is {maxHtmlLength} characters.");
            }

            var title = string.IsNullOrWhiteSpace(request.Title) ? "Newsletter" : request.Title;
            var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl)
                ? "https://warlinghamcc.co.uk"
                : request.BaseUrl;

            var pdfBytes = await _pdfService.GeneratePdfAsync(
                request.Html,
                title,
                baseUrl,
                cancellationToken
            );

            var safeFileName = MakeSafeFileName(title);

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";

            return File(pdfBytes, "application/pdf", $"{safeFileName}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF generation failed.");
            return StatusCode(500, new
            {
                message = "PDF generation failed.",
                detail = ex.Message
            });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            service = "warlinghamcc.PdfService",
            time = DateTimeOffset.UtcNow
        });
    }

    private IActionResult? ValidateApiKey()
    {
        var enableApiKey = _configuration.GetValue<bool?>("Pdf:EnableApiKey") ?? true;
        if (!enableApiKey)
        {
            return null;
        }

        var configuredKey = _configuration["Pdf:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey) || configuredKey == "test123.")
        {
            return StatusCode(500, "PDF API key is not configured.");
        }

        if (!Request.Headers.TryGetValue("x-api-key", out var suppliedKey) || suppliedKey != configuredKey)
        {
            return Unauthorized("Invalid or missing PDF API key.");
        }

        return null;
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '-');
        }

        return string.IsNullOrWhiteSpace(name) ? "newsletter" : name.Trim();
    }
}
