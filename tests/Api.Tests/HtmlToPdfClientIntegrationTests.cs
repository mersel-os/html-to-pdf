using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Api.Tests.Fixtures;
using MERSEL.Services.HtmlToPdf.Client.Interfaces;
using MERSEL.Services.HtmlToPdf.Client;
using MERSEL.Services.HtmlToPdf.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MERSEL.Services.HtmlToPdf.Api.Tests;

/// <summary>
/// HtmlToPdfClient SDK uçtan uca entegrasyon testleri.
/// Gerçek WebApplicationFactory sunucusuna HTTP çağrısı yapar.
/// İstemci SDK'sının API ile doğru iletişim kurduğunu doğrular.
/// </summary>
public class HtmlToPdfClientIntegrationTests : IClassFixture<HtmlToPdfWebAppFactory>
{
    private readonly IHtmlToPdfConverter _client;

    public HtmlToPdfClientIntegrationTests(HtmlToPdfWebAppFactory factory)
    {
        // Test HTTP istemcisini WebApplicationFactory'den al
        var httpClient = factory.CreateClient();

        // HtmlToPdfClient normalde IHttpClientFactory kullanır.
        // Test ortamında doğrudan HttpClient enjekte eden bir mock factory kullanıyoruz.
        var httpClientFactory = new TestHttpClientFactory(httpClient);

        _client = new HtmlToPdfClient(httpClientFactory, NullLogger<HtmlToPdfClient>.Instance);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ConvertAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_ShouldReturnValidPdf_ThroughClientSdk()
    {
        // Arrange
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.MinimalHtml);

        // Act
        var pdfBytes = await _client.ConvertAsync(htmlBytes);

        // Assert — SDK üzerinden gelen PDF geçerli olmalı
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task ConvertAsync_ShouldHandleStyledHtml_ThroughClientSdk()
    {
        // Arrange
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.StyledHtml);

        // Act
        var pdfBytes = await _client.ConvertAsync(htmlBytes);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task ConvertAsync_ShouldHandleInvoiceHtml_ThroughClientSdk()
    {
        // Arrange — gerçekçi fatura şablonu
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.InvoiceHtml);

        // Act
        var pdfBytes = await _client.ConvertAsync(htmlBytes);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 1);
    }

    [Fact]
    public async Task ConvertAsync_ShouldThrowHttpRequestException_WhenSendingEmptyContent()
    {
        // Arrange — boş byte dizisi (API 400 döner, SDK HttpRequestException fırlatır)
        var emptyBytes = Array.Empty<byte>();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _client.ConvertAsync(emptyBytes));
    }

    // ════════════════════════════════════════════════════════════════════════
    // ConvertAndMergeAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldReturnMergedPdf_ThroughClientSdk()
    {
        // Arrange
        var htmls = new[]
        {
            HtmlSamples.ToBytes(HtmlSamples.CreateColoredPageHtml("#e74c3c", "Red")),
            HtmlSamples.ToBytes(HtmlSamples.CreateColoredPageHtml("#2ecc71", "Green")),
            HtmlSamples.ToBytes(HtmlSamples.CreateColoredPageHtml("#3498db", "Blue"))
        };

        // Act
        var pdfBytes = await _client.ConvertAndMergeAsync(htmls);

        // Assert
        PdfAssert.ShouldHavePageCount(pdfBytes, 3);
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldReturnValidPdf_WhenGivenSingleHtml()
    {
        // Arrange — tek dosya birleştirme
        var htmls = new[] { HtmlSamples.ToBytes(HtmlSamples.MinimalHtml) };

        // Act
        var pdfBytes = await _client.ConvertAndMergeAsync(htmls);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldHandleManyFiles_ThroughClientSdk()
    {
        // Arrange — 5 dosya birleştirme
        var htmls = Enumerable.Range(1, 5)
            .Select(i => HtmlSamples.ToBytes(HtmlSamples.CreateNumberedPageHtml(i)))
            .ToArray();

        // Act
        var pdfBytes = await _client.ConvertAndMergeAsync(htmls);

        // Assert
        PdfAssert.ShouldHavePageCount(pdfBytes, 5);
    }

    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Test ortamında WebApplicationFactory'nin HttpClient'ını
    /// IHttpClientFactory arayüzü üzerinden sağlayan yardımcı sınıf.
    /// </summary>
    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
