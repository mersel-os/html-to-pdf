using MERSEL.Services.HtmlToPdf.Application.Interfaces;
using MERSEL.Services.HtmlToPdf.Infrastructure;
using MERSEL.Services.HtmlToPdf.Infrastructure.Diagnostics;
using MERSEL.Services.HtmlToPdf.Web;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Yapılandırılmış Loglama ────────────────────────────────────────
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ── OpenAPI / Scalar ────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── OpenTelemetry Metrikler ─────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddMeter(HtmlToPdfMetrics.MeterName);
        metrics.AddPrometheusExporter();
    });

// ── Altyapı Katmanı ────────────────────────────────────────────────
builder.Services.AddHtmlToPdfInfrastructure();

// ── Chromium Başlangıç Kontrolü ─────────────────────────────────────
PlaywrightPdfConverter.EnsureBrowserInstalled();

var app = builder.Build();

// ── OpenAPI JSON ────────────────────────────────────────────────────
app.MapOpenApi();

// ── Scalar API Dokümantasyon Arayüzü ────────────────────────────────
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("MERSEL HtmlToPdf API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// ── Prometheus Metrik Endpoint'i ────────────────────────────────────
app.MapPrometheusScrapingEndpoint();

// ── Sağlık Kontrolü (/health) ───────────────────────────────────────
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            Status = report.Status.ToString(),
            Service = "HtmlToPdf",
            Duration = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration.TotalMilliseconds
            })
        });
    }
})
.WithName("HealthCheck")
.WithTags("Health")
.WithDescription("Servisin ve Chromium tarayıcısının durumunu kontrol eder")
.WithSummary("Sağlık kontrolü");

// ══════════════════════════════════════════════════════════════════════
// POST /convert — Tek HTML → PDF
//
// Body: multipart/form-data → "file" alanı (zorunlu)
// Query: ?landscape=true&scale=0.8&marginTop=25mm
// ══════════════════════════════════════════════════════════════════════

app.MapPost("/convert", async (
    IHtmlToPdfConverter converter,
    [FromForm] IFormFile file,
    [AsParameters] PdfOptionsQuery query,
    CancellationToken ct) =>
{
    if (file.Length == 0)
        return Results.BadRequest(new { Error = "HTML dosyası boş olamaz" });

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms, ct);
    var htmlBytes = ms.ToArray();

    var options = query.ToOptions();
    var pdfBytes = await converter.ConvertAsync(htmlBytes, options, ct);
    return Results.File(pdfBytes, "application/pdf", "document.pdf");
})
.WithName("ConvertHtmlToPdf")
.WithTags("Convert")
.WithDescription("""
    Tek bir HTML belgesini PDF'e dönüştürür.
    
    HTML içeriği: multipart/form-data "file" alanı olarak gönderilir.
    PDF ayarları: query string parametreleri (format, landscape, scale, marginTop vb.)
    """)
.WithSummary("HTML → PDF dönüşümü")
.Produces(200, contentType: "application/pdf")
.ProducesProblem(400)
.DisableAntiforgery();

// ══════════════════════════════════════════════════════════════════════
// POST /convert/merge — Çoklu HTML → Birleştirilmiş PDF
//
// Body: multipart/form-data → birden fazla dosya alanı
// Query: ?landscape=true&scale=0.8
// ══════════════════════════════════════════════════════════════════════

app.MapPost("/convert/merge", async (
    IHtmlToPdfConverter converter,
    [FromForm] IFormFileCollection files,
    [AsParameters] PdfOptionsQuery query,
    CancellationToken ct) =>
{
    var htmlContentsList = new List<byte[]>();

    foreach (var file in files)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (bytes.Length > 0)
            htmlContentsList.Add(bytes);
    }

    if (htmlContentsList.Count == 0)
        return Results.BadRequest(new { Error = "En az bir HTML dosyası gereklidir" });

    var options = query.ToOptions();
    var pdfBytes = await converter.ConvertAndMergeAsync(htmlContentsList, options, ct);
    return Results.File(pdfBytes, "application/pdf", "merged.pdf");
})
.WithName("ConvertAndMergeHtmlToPdf")
.WithTags("Convert")
.WithDescription("""
    Birden fazla HTML belgesini tek bir birleştirilmiş PDF'e dönüştürür.
    
    HTML dosyaları: multipart/form-data ile gönderilir (birden fazla dosya).
    PDF ayarları: query string parametreleri (tüm belgelere uygulanır).
    """)
.WithSummary("Çoklu HTML → Birleştirilmiş PDF")
.Produces(200, contentType: "application/pdf")
.ProducesProblem(400)
.DisableAntiforgery();

app.Run();

// WebApplicationFactory entegrasyon testleri için gerekli
public partial class Program;
