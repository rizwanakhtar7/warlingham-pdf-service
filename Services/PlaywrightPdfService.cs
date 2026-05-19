using Microsoft.Playwright;

namespace warlinghamcc.PdfService.Services;

public class PlaywrightPdfService : IPlaywrightPdfService, IAsyncDisposable
{
    private static readonly SemaphoreSlim PdfLock = new(1, 1);

    private readonly NewsletterPdfHtmlService _htmlService;
    private readonly ILogger<PlaywrightPdfService> _logger;
    private readonly SemaphoreSlim _browserLock = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public PlaywrightPdfService(
        NewsletterPdfHtmlService htmlService,
        ILogger<PlaywrightPdfService> logger)
    {
        _htmlService = htmlService;
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfAsync(
        string html,
        string title,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        await PdfLock.WaitAsync(cancellationToken);

        try
        {
            var browser = await GetBrowserAsync();

            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = 794,
                    Height = 1123
                },
                DeviceScaleFactor = 1,
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(30000);

            await page.RouteAsync("**/*", async route =>
            {
                var resourceType = route.Request.ResourceType;

                if (resourceType == "media" || resourceType == "font")
                {
                    await route.AbortAsync();
                    return;
                }

                await route.ContinueAsync();
            });

            var fullHtml = _htmlService.PrepareHtml(html, title, baseUrl);

await page.SetContentAsync(fullHtml, new PageSetContentOptions
{
    WaitUntil = WaitUntilState.NetworkIdle,
    Timeout = 30000
});

await page.EvaluateAsync("""
    async () => {
        if (window.twemoji) {
            twemoji.parse(document.body, {
                folder: 'svg',
                ext: '.svg',
                base: 'https://cdn.jsdelivr.net/gh/twitter/twemoji@16.0.1/assets/'
            });
        }

        const images = Array.from(document.images);

        await Promise.all(images.map(img => {
            if (img.complete && img.naturalWidth > 0) {
                return Promise.resolve();
            }

            return new Promise(resolve => {
                img.onload = resolve;
                img.onerror = resolve;
            });
        }));

        Array.from(document.querySelectorAll('a[href]')).forEach(a => {
            a.style.display = 'inline-block';
            a.setAttribute('target', '_blank');
            a.setAttribute('rel', 'noopener noreferrer');
        });
    }
""");

await page.WaitForTimeoutAsync(500);

            var pdfBytes = await page.PdfAsync(new PagePdfOptions
            {
                Format = "A4",
                PrintBackground = true,
                PreferCSSPageSize = true,
                Margin = new Margin
                {
                    Top = "0",
                    Bottom = "0",
                    Left = "0",
                    Right = "0"
                }
            });

            await page.CloseAsync();
            return pdfBytes;
        }
        finally
        {
            PdfLock.Release();
        }
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser != null && _browser.IsConnected)
        {
            return _browser;
        }

        await _browserLock.WaitAsync();

        try
        {
            if (_browser != null && _browser.IsConnected)
            {
                return _browser;
            }

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                }
            });

            _logger.LogInformation("Playwright Chromium launched.");
            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
        PdfLock.Dispose();
        _browserLock.Dispose();
    }
}
