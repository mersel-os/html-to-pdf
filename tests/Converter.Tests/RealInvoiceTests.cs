using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Application.Models;
using MERSEL.Services.HtmlToPdf.Converter.Tests.Fixtures;
using MERSEL.Services.HtmlToPdf.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace MERSEL.Services.HtmlToPdf.Converter.Tests;

/// <summary>
/// Gerçek e-fatura HTML şablonlarıyla dönüşüm testleri.
/// Üretim ortamındaki gerçek belgelerin (QR kod JS, karmaşık CSS, tablo yapıları)
/// doğru render edildiğini ve PDF çıktılarının geçerli olduğunu doğrular.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public class RealInvoiceTests
{
    private readonly PlaywrightFixture _fixture;
    private readonly ITestOutputHelper _output;

    public RealInvoiceTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tekli Dönüşüm — Her fatura tipi
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_ShouldProduceValidPdf_ForTeknolojiFatura()
    {
        var pdfBytes = await _fixture.Converter.ConvertAsync(HtmlSamples.TeknolojiFaturaBytes);

        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 1);
        _output.WriteLine($"TeknolojiFatura: {pdfBytes.Length:N0} byte, {PdfAssert.GetPageCount(pdfBytes)} sayfa");
    }

    [Fact]
    public async Task ConvertAsync_ShouldProduceValidPdf_ForTemelFatura()
    {
        var pdfBytes = await _fixture.Converter.ConvertAsync(HtmlSamples.TemelFaturaBytes);

        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 1);
        _output.WriteLine($"TemelFatura: {pdfBytes.Length:N0} byte, {PdfAssert.GetPageCount(pdfBytes)} sayfa");
    }

    [Fact]
    public async Task ConvertAsync_ShouldProduceValidPdf_ForTicariFatura()
    {
        var pdfBytes = await _fixture.Converter.ConvertAsync(HtmlSamples.TicariFaturaBytes);

        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 1);
        _output.WriteLine($"TicariFatura: {pdfBytes.Length:N0} byte, {PdfAssert.GetPageCount(pdfBytes)} sayfa");
    }

    // ════════════════════════════════════════════════════════════════════════
    // SmartShrinking ile dönüşüm
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_WithSmartShrinking_ShouldProduceValidPdf_ForAllInvoiceTypes()
    {
        var options = PdfConvertOptions.WkHtmlCompatible;

        foreach (var (name, html) in GetNamedInvoices())
        {
            var pdfBytes = await _fixture.Converter.ConvertAsync(html, options);

            PdfAssert.ShouldBeValidPdf(pdfBytes);
            _output.WriteLine($"{name} (SmartShrink): {pdfBytes.Length:N0} byte, {PdfAssert.GetPageCount(pdfBytes)} sayfa");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // Birleştirme — Gerçek faturalar
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldMergeAllRealInvoices_IntoSinglePdf()
    {
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(HtmlSamples.AllRealInvoices);

        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 3);
        _output.WriteLine($"3 fatura birleştirildi: {pdfBytes.Length:N0} byte, {PdfAssert.GetPageCount(pdfBytes)} sayfa");
    }

    [Fact]
    public async Task ConvertAndMergeAsync_WithSmartShrinking_ShouldMergeAllRealInvoices()
    {
        var options = PdfConvertOptions.WkHtmlCompatible;
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(HtmlSamples.AllRealInvoices, options);

        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 3);
        _output.WriteLine($"3 fatura birleştirildi (SmartShrink): {pdfBytes.Length:N0} byte, {PdfAssert.GetPageCount(pdfBytes)} sayfa");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Yardımcılar
    // ════════════════════════════════════════════════════════════════════════

    private static IEnumerable<(string Name, byte[] Html)> GetNamedInvoices()
    {
        yield return ("TeknolojiFatura", HtmlSamples.TeknolojiFaturaBytes);
        yield return ("TemelFatura", HtmlSamples.TemelFaturaBytes);
        yield return ("TicariFatura", HtmlSamples.TicariFaturaBytes);
    }
}
