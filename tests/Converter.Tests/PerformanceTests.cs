using System.Diagnostics;
using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Application.Models;
using MERSEL.Services.HtmlToPdf.Converter.Tests.Fixtures;
using MERSEL.Services.HtmlToPdf.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace MERSEL.Services.HtmlToPdf.Converter.Tests;

/// <summary>
/// Performans ve stres testleri.
/// Gerçek e-fatura şablonlarıyla sıralı/eşzamanlı dönüşüm süreleri ve
/// throughput ölçümlerini yaparak üretim ortamında beklenen yükü simüle eder.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public class PerformanceTests
{
    private readonly PlaywrightFixture _fixture;
    private readonly ITestOutputHelper _output;

    public PerformanceTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tekli Dönüşüm Performansı
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_SingleInvoice_ShouldCompleteWithin10Seconds()
    {
        // Warmup — ilk dönüşüm Chromium context oluşturma maliyetini içerir
        await _fixture.Converter.ConvertAsync(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        var sw = Stopwatch.StartNew();
        var pdfBytes = await _fixture.Converter.ConvertAsync(HtmlSamples.TicariFaturaBytes);
        sw.Stop();

        PdfAssert.ShouldBeValidPdf(pdfBytes);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(10,
            "tek bir gerçek fatura 10 saniyenin altında dönüştürülmeli");

        _output.WriteLine($"Tekli dönüşüm: {sw.Elapsed.TotalMilliseconds:F0}ms, {pdfBytes.Length:N0} byte");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Sıralı Dönüşüm Throughput
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_Sequential10Invoices_ShouldMeasureThroughput()
    {
        // Warmup
        await _fixture.Converter.ConvertAsync(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        var invoices = HtmlSamples.AllRealInvoices;
        var totalBytes = 0L;
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 10; i++)
        {
            var html = invoices[i % invoices.Length];
            var pdf = await _fixture.Converter.ConvertAsync(html);
            PdfAssert.ShouldBeValidPdf(pdf);
            totalBytes += pdf.Length;
        }

        sw.Stop();

        var avgMs = sw.Elapsed.TotalMilliseconds / 10;
        var throughputPerSec = 10.0 / sw.Elapsed.TotalSeconds;

        _output.WriteLine("═══ Sıralı Dönüşüm (10 fatura) ═══");
        _output.WriteLine($"Toplam süre : {sw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Ortalama    : {avgMs:F0}ms/fatura");
        _output.WriteLine($"Throughput  : {throughputPerSec:F2} fatura/sn");
        _output.WriteLine($"Toplam PDF  : {totalBytes:N0} byte");

        avgMs.Should().BeLessThan(10_000, "ortalama dönüşüm süresi 10 saniyenin altında olmalı");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Eşzamanlı Dönüşüm Stres Testi
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConvertAsync_ConcurrentInvoices_ShouldAllSucceed(int concurrency)
    {
        // Warmup
        await _fixture.Converter.ConvertAsync(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        var invoices = HtmlSamples.AllRealInvoices;
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrency)
            .Select(i => _fixture.Converter.ConvertAsync(invoices[i % invoices.Length]))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        results.Should().HaveCount(concurrency);
        foreach (var pdf in results)
            PdfAssert.ShouldBeValidPdf(pdf);

        var avgMs = sw.Elapsed.TotalMilliseconds / concurrency;
        _output.WriteLine($"═══ Eşzamanlı ({concurrency} fatura) ═══");
        _output.WriteLine($"Toplam süre  : {sw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Ort/fatura   : {avgMs:F0}ms");
        _output.WriteLine($"Throughput   : {concurrency / sw.Elapsed.TotalSeconds:F2} fatura/sn");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Birleştirme Performansı
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAndMergeAsync_3RealInvoices_ShouldMeasurePerformance()
    {
        // Warmup
        await _fixture.Converter.ConvertAsync(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        var sw = Stopwatch.StartNew();
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(HtmlSamples.AllRealInvoices);
        sw.Stop();

        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 3);

        _output.WriteLine($"═══ 3 Fatura Birleştirme ═══");
        _output.WriteLine($"Süre      : {sw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"PDF boyut : {pdfBytes.Length:N0} byte");
        _output.WriteLine($"Sayfa     : {PdfAssert.GetPageCount(pdfBytes)}");
    }

    [Fact]
    public async Task ConvertAndMergeAsync_10RealInvoices_ShouldCompleteWithinReasonableTime()
    {
        // Warmup
        await _fixture.Converter.ConvertAsync(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        // 10 gerçek fatura birleştirme (3 şablon döngüsel)
        var invoices = Enumerable.Range(0, 10)
            .Select(i => HtmlSamples.AllRealInvoices[i % 3])
            .ToArray();

        var sw = Stopwatch.StartNew();
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(invoices);
        sw.Stop();

        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 10);

        _output.WriteLine($"═══ 10 Fatura Birleştirme ═══");
        _output.WriteLine($"Süre      : {sw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Ort/fatura: {sw.Elapsed.TotalMilliseconds / 10:F0}ms");
        _output.WriteLine($"PDF boyut : {pdfBytes.Length:N0} byte");
        _output.WriteLine($"Sayfa     : {PdfAssert.GetPageCount(pdfBytes)}");

        sw.Elapsed.TotalMinutes.Should().BeLessThan(2,
            "10 gerçek fatura birleştirme 2 dakikayı geçmemeli");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Eşzamanlı Birleştirme Stres
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAndMergeAsync_ConcurrentMergeRequests_ShouldAllSucceed()
    {
        // Warmup
        await _fixture.Converter.ConvertAsync(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        // 3 eşzamanlı birleştirme isteği, her biri 3 gerçek fatura
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, 3)
            .Select(_ => _fixture.Converter.ConvertAndMergeAsync(HtmlSamples.AllRealInvoices))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        results.Should().HaveCount(3);
        foreach (var pdf in results)
        {
            PdfAssert.ShouldBeValidPdf(pdf);
            PdfAssert.ShouldHaveAtLeastPages(pdf, 3);
        }

        _output.WriteLine($"═══ 3 Eşzamanlı Birleştirme (her biri 3 fatura) ═══");
        _output.WriteLine($"Toplam süre : {sw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Throughput  : {9.0 / sw.Elapsed.TotalSeconds:F2} fatura/sn");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Bellek ve Boyut Profili
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_AllInvoiceTypes_ShouldMeasureSizeProfile()
    {
        _output.WriteLine("═══ PDF Boyut Profili ═══");
        _output.WriteLine($"{"Şablon",-20} {"HTML (KB)",10} {"PDF (KB)",10} {"Oran",8} {"Sayfa",6}");
        _output.WriteLine(new string('─', 58));

        foreach (var (name, html) in new[]
                 {
                     ("TeknolojiFatura", HtmlSamples.TeknolojiFaturaBytes),
                     ("TemelFatura", HtmlSamples.TemelFaturaBytes),
                     ("TicariFatura", HtmlSamples.TicariFaturaBytes)
                 })
        {
            var pdf = await _fixture.Converter.ConvertAsync(html);
            PdfAssert.ShouldBeValidPdf(pdf);

            var htmlKb = html.Length / 1024.0;
            var pdfKb = pdf.Length / 1024.0;
            var ratio = pdfKb / htmlKb;
            var pages = PdfAssert.GetPageCount(pdf);

            _output.WriteLine($"{name,-20} {htmlKb,10:F1} {pdfKb,10:F1} {ratio,7:F2}x {pages,5}");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SmartShrinking Karşılaştırma
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_SmartShrinking_ShouldReducePageCount_OrMaintainSize()
    {
        _output.WriteLine("═══ SmartShrinking Karşılaştırma ═══");
        _output.WriteLine($"{"Şablon",-20} {"Normal (s/b)",15} {"Shrink (s/b)",15} {"Fark",8}");
        _output.WriteLine(new string('─', 62));

        var options = PdfConvertOptions.WkHtmlCompatible;

        foreach (var (name, html) in new[]
                 {
                     ("TeknolojiFatura", HtmlSamples.TeknolojiFaturaBytes),
                     ("TemelFatura", HtmlSamples.TemelFaturaBytes),
                     ("TicariFatura", HtmlSamples.TicariFaturaBytes)
                 })
        {
            var normalPdf = await _fixture.Converter.ConvertAsync(html);
            var shrinkPdf = await _fixture.Converter.ConvertAsync(html, options);

            PdfAssert.ShouldBeValidPdf(normalPdf);
            PdfAssert.ShouldBeValidPdf(shrinkPdf);

            var normalPages = PdfAssert.GetPageCount(normalPdf);
            var shrinkPages = PdfAssert.GetPageCount(shrinkPdf);

            _output.WriteLine(
                $"{name,-20} {normalPages}s/{normalPdf.Length / 1024.0:F0}KB {shrinkPages,5}s/{shrinkPdf.Length / 1024.0:F0}KB {(shrinkPages <= normalPages ? "OK" : "!"),7}");

            // SmartShrinking hiçbir zaman sayfa sayısını artırmamalı
            shrinkPages.Should().BeLessOrEqualTo(normalPages,
                $"{name}: SmartShrinking sayfa sayısını artırmamalı");
        }
    }
}
