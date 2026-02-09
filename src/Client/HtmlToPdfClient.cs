using System.Globalization;
using MERSEL.Services.HtmlToPdf.Application.Interfaces;
using MERSEL.Services.HtmlToPdf.Application.Models;
using Microsoft.Extensions.Logging;

namespace MERSEL.Services.HtmlToPdf.Client;

/// <summary>
/// MERSEL HtmlToPdf mikroservisini HTTP üzerinden çağıran istemci.
/// <see cref="DependencyInjection.AddHtmlToPdfClient(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration)"/>
/// ile DI'ye kaydedilir ve <see cref="IHtmlToPdfConverter"/> olarak enjekte edilir.
/// </summary>
public sealed class HtmlToPdfClient : IHtmlToPdfConverter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HtmlToPdfClient> _logger;

    public HtmlToPdfClient(
        IHttpClientFactory httpClientFactory,
        ILogger<HtmlToPdfClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("HtmlToPdf");
        _logger = logger;
    }

    // ── Varsayılan ayarlar (options'sız overload'lar) ────────────────

    /// <inheritdoc />
    public async Task<byte[]> ConvertAsync(byte[] htmlContent, CancellationToken ct = default)
    {
        _logger.LogDebug("HtmlToPdf servisine tekli HTML gönderiliyor ({Boyut} byte)", htmlContent.Length);

        using var formContent = new MultipartFormDataContent();
        var htmlPart = new ByteArrayContent(htmlContent);
        htmlPart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
        formContent.Add(htmlPart, "file", "document.html");

        var response = await _httpClient.PostAsync("/convert", formContent, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<byte[]> ConvertAndMergeAsync(IEnumerable<byte[]> htmlContents, CancellationToken ct = default)
    {
        using var formContent = new MultipartFormDataContent();
        var index = 0;

        foreach (var html in htmlContents)
        {
            var part = new ByteArrayContent(html);
            part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            formContent.Add(part, $"file{index}", $"document{index}.html");
            index++;
        }

        _logger.LogDebug("HtmlToPdf servisine {Adet} HTML belgesi gönderiliyor", index);

        var response = await _httpClient.PostAsync("/convert/merge", formContent, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // ── Özel ayarlar (options'lu overload'lar) ───────────────────────

    /// <inheritdoc />
    public async Task<byte[]> ConvertAsync(byte[] htmlContent, PdfConvertOptions options, CancellationToken ct = default)
    {
        _logger.LogDebug("HtmlToPdf servisine tekli HTML gönderiliyor ({Boyut} byte, özel ayarlar)", htmlContent.Length);

        using var formContent = new MultipartFormDataContent();
        var htmlPart = new ByteArrayContent(htmlContent);
        htmlPart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
        formContent.Add(htmlPart, "file", "document.html");

        var qs = BuildQueryString(options);
        var response = await _httpClient.PostAsync($"/convert{qs}", formContent, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<byte[]> ConvertAndMergeAsync(IEnumerable<byte[]> htmlContents, PdfConvertOptions options, CancellationToken ct = default)
    {
        using var formContent = new MultipartFormDataContent();
        var index = 0;

        foreach (var html in htmlContents)
        {
            var part = new ByteArrayContent(html);
            part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            formContent.Add(part, $"file{index}", $"document{index}.html");
            index++;
        }

        _logger.LogDebug("HtmlToPdf servisine {Adet} HTML belgesi gönderiliyor (özel ayarlar)", index);

        var qs = BuildQueryString(options);
        var response = await _httpClient.PostAsync($"/convert/merge{qs}", formContent, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // ── Query String Builder ────────────────────────────────────────

    private static string BuildQueryString(PdfConvertOptions o)
    {
        var d = new PdfConvertOptions(); // varsayılanlar
        var p = new List<string>();

        void Add(string key, string? val, string? def) { if (val != def && !string.IsNullOrEmpty(val)) p.Add($"{key}={Uri.EscapeDataString(val)}"); }
        void AddBool(string key, bool val, bool def) { if (val != def) p.Add($"{key}={val.ToString().ToLowerInvariant()}"); }
        void AddFloat(string key, float val, float def) { if (Math.Abs(val - def) > 0.001f) p.Add($"{key}={val.ToString("F2", CultureInfo.InvariantCulture)}"); }
        void AddInt(string key, int? val) { if (val is > 0) p.Add($"{key}={val}"); }

        Add("format", o.Format, d.Format);
        AddBool("landscape", o.Landscape, d.Landscape);
        AddFloat("scale", o.Scale, d.Scale);
        AddBool("printBackground", o.PrintBackground, d.PrintBackground);
        Add("marginTop", o.MarginTop, d.MarginTop);
        Add("marginBottom", o.MarginBottom, d.MarginBottom);
        Add("marginLeft", o.MarginLeft, d.MarginLeft);
        Add("marginRight", o.MarginRight, d.MarginRight);
        AddBool("displayHeaderFooter", o.DisplayHeaderFooter, d.DisplayHeaderFooter);
        Add("headerTemplate", o.HeaderTemplate, null);
        Add("footerTemplate", o.FooterTemplate, null);
        AddInt("jsDelay", o.JavaScriptDelayMs);
        AddBool("waitForNetworkIdle", o.WaitForNetworkIdle, d.WaitForNetworkIdle);
        AddBool("preferCssPageSize", o.PreferCssPageSize, d.PreferCssPageSize);
        Add("pageRanges", o.PageRanges, null);
        Add("width", o.Width, null);
        Add("height", o.Height, null);
        AddBool("smartShrinking", o.SmartShrinking, d.SmartShrinking);

        return p.Count > 0 ? "?" + string.Join("&", p) : "";
    }
}
