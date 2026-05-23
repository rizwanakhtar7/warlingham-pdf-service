using System.Threading.RateLimiting;
using warlinghamcc.PdfService.Services;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<NewsletterPdfHtmlService>();
builder.Services.AddSingleton<IPlaywrightPdfService, PlaywrightPdfService>();

var allowedOrigins =
    builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("AllowedOrigins is not configured.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var rateLimitPerMinute = builder.Configuration.GetValue<int>(
    "Pdf:RateLimitPerMinute",
    3
);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var apiKey = httpContext.Request.Headers["X-API-KEY"].FirstOrDefault();

        var partitionKey = !string.IsNullOrWhiteSpace(apiKey)
            ? $"apikey:{apiKey}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = rateLimitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }
        );
    });
});


var app = builder.Build();


if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Pdf:EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendOnly");

app.UseRateLimiter();
app.MapControllers();

app.Run();
