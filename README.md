# Warlingham CC PDF Service

ASP.NET Core 6 PDF microservice for newsletter HTML to PDF generation using Playwright.

## Local setup

```bash
dotnet restore
dotnet build
powershell -ExecutionPolicy Bypass -File bin\Debug\net6.0\playwright.ps1 install chromium
dotnet run
```

Swagger:

```txt
https://localhost:7044/swagger
```

Health check:

```txt
https://localhost:7044/api/pdf/health
```

## Test request

POST:

```txt
/api/pdf/generate
```

Headers in production:

```txt
Content-Type: application/json
Accept: application/pdf
x-api-key: YOUR_SECRET
```

Body:

```json
{
  "title": "The Warls Wire",
  "baseUrl": "https://warlinghamcc.co.uk",
  "html": "<h1>Hello</h1><a href="https://warlinghamcc.co.uk">Website</a>"
}
```

## Production notes

- Change `Pdf:ApiKey` in `appsettings.json` or via environment variables.
- Keep `Pdf:EnableApiKey` true in production.
- Allowed origins should include only your real frontend domain.
- The service limits HTML size and PDF generation concurrency. For .NET 6 compatibility, it uses a SemaphoreSlim rather than ASP.NET Core rate-limiting middleware.
- Emojis are replaced backend-only so the frontend/email template can stay unchanged.

## Playwright production

Playwright needs Chromium installed on the server. For Docker/Render/Railway, use the included Dockerfile.

For shared hosting, Playwright may not work if the host blocks browser execution. If that happens, host this service separately using Docker.
