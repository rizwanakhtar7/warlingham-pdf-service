using System.Net;
using System.Text.RegularExpressions;
namespace warlinghamcc.PdfService.Services;

public class NewsletterPdfHtmlService
{
    public string PrepareHtml(string html, string title, string baseUrl)
    {
        html = string.IsNullOrWhiteSpace(html) ? "<body></body>" : html;

        html = RemoveDangerousTags(html);
        html = EnsureHtmlDocument(html, title);
        html = MakeLinksAndAssetsAbsolute(html, baseUrl);
        html = ReplaceEmojisForPdf(html);
        html = StripPlayerSpotlightForPdf(html);
        html = InjectPrintCss(html);

        return html;
    }

    private static string EnsureHtmlDocument(string html, string title)
    {
        if (Regex.IsMatch(html, "<html[\\s>]", RegexOptions.IgnoreCase))
        {
            return html;
        }

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{WebUtility.HtmlEncode(title)}</title>
</head>
<body>
    {html}
</body>
</html>";
    }

    private static string RemoveDangerousTags(string html)
    {
        html = Regex.Replace(html, @"<script[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<iframe[\s\S]*?</iframe>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<object[\s\S]*?</object>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<embed[\s\S]*?</embed>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<video[\s\S]*?</video>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<audio[\s\S]*?</audio>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\s(onerror|onclick|onload|onmouseover|onfocus)\s*=\s*(['""`])[^'""`]*\2", string.Empty, RegexOptions.IgnoreCase);
        return html;
    }

    private static string ReplaceEmojisForPdf(string html)
    {
        var replacements = new Dictionary<string, string>
        {
            ["✨"] = "&#9733;",
            ["⭐"] = "&#9733;",
            ["🎉"] = "&#9733;",
            ["🔧"] = "&#9670;",
            ["🍺"] = "&#9679;",
            ["🧢"] = "WCC"
        };

        foreach (var item in replacements)
        {
            html = html.Replace(item.Key, item.Value);
        }

        return html;
    }

    private static string MakeLinksAndAssetsAbsolute(string html, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://warlinghamcc.co.uk";
        }

        baseUrl = baseUrl.TrimEnd('/');

        return Regex.Replace(
            html,
            "(href|src)=[\"'](?!https?:\\/\\/|mailto:|data:|#)([^\"']+)[\"']",
            match =>
            {
                var attr = match.Groups[1].Value;
                var value = match.Groups[2].Value.TrimStart('/');
                return $@"{attr}=""{baseUrl}/{value}""";
            },
            RegexOptions.IgnoreCase
        );
    }

    private static string StripPlayerSpotlightForPdf(string html)
    {
        var next = html;

        next = Regex.Replace(
            next,
            @"<table[^>]*>\s*<tr>\s*<td[^>]*>\s*<table[^>]*>\s*<tr>\s*<td[^>]*>\s*Player Spotlight\s*</td>[\s\S]*?</table>\s*</td>\s*</tr>\s*</table>\s*",
            string.Empty,
            RegexOptions.IgnoreCase
        );

        next = Regex.Replace(
            next,
            @"<table[^>]*style=""[^""]*background-color:\s*#2a0d07[^""]*border-radius:\s*12px[^""]*""[^>]*>[\s\S]*?(?=<table[^>]*>\s*<tr>\s*<td[^>]*style=""[^""]*padding:\s*32px 0 0 0)",
            string.Empty,
            RegexOptions.IgnoreCase
        );

        return next;
    }

    private static string InjectPrintCss(string html)
    {
        const string css = @"
<style>
    @page {
        size: A4;
        margin: 0;
    }

    html,
    body {
        margin: 0 !important;
        padding: 0 !important;
        background: #c9b89a !important;
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
    }

    * {
        box-sizing: border-box !important;
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
    }

    body {
        font-family: Arial, Helvetica, sans-serif !important;
    }

    table {
        border-collapse: collapse !important;
    }

    img {
        max-width: 100% !important;
        height: auto !important;
        border: 0 !important;
        outline: none !important;
        text-decoration: none !important;
        display: block !important;
    }

    a,
    a:visited,
    a:hover,
    a:active {
        color: inherit !important;
        text-decoration: none !important;
    }

    td,
    div,
    p,
    span,
    a {
        word-break: break-word !important;
        overflow-wrap: break-word !important;
        word-wrap: break-word !important;
    }

    .container {
        width: 620px !important;
        max-width: 620px !important;
        min-width: 620px !important;
        margin-left: auto !important;
        margin-right: auto !important;
    }
</style>
";

        if (Regex.IsMatch(html, "</head>", RegexOptions.IgnoreCase))
        {
            return Regex.Replace(html, "</head>", css + "</head>", RegexOptions.IgnoreCase);
        }

        return html;
    }
}
