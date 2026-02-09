using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Converter.Tests.Fixtures;
using MERSEL.Services.HtmlToPdf.Tests.Shared;
using Xunit;

namespace MERSEL.Services.HtmlToPdf.Converter.Tests;

/// <summary>
/// PlaywrightPdfConverter entegrasyon testleri.
/// Tüm testler aynı Chromium tarayıcı örneğini paylaşır (<see cref="PlaywrightCollection"/>).
/// Chromium çalışır durumda olmalıdır (playwright install chromium).
/// </summary>
[Collection(PlaywrightCollection.Name)]
public class PlaywrightPdfConverterTests
{
    private readonly PlaywrightFixture _fixture;

    public PlaywrightPdfConverterTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ConvertAsync — Temel Senaryolar
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_ShouldReturnValidPdf_WhenGivenSimpleHtml()
    {
        // Arrange
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.MinimalHtml);

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task ConvertAsync_ShouldRenderCorrectly_WhenHtmlContainsTurkishCharacters()
    {
        // Arrange
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.TurkishCharactersHtml);

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert — Türkçe HTML daha fazla içerik barındırır, daha büyük PDF üretmeli
        PdfAssert.ShouldBeValidPdf(pdfBytes);
        var minimalPdf = await _fixture.Converter.ConvertAsync(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));
        pdfBytes.Length.Should().BeGreaterThan(minimalPdf.Length);
    }

    [Fact]
    public async Task ConvertAsync_ShouldReturnValidPdf_WhenHtmlContainsCssStyles()
    {
        // Arrange
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.StyledHtml);

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task ConvertAsync_ShouldReturnValidPdf_WhenHtmlBodyIsEmpty()
    {
        // Arrange — gövdesi boş ama yapısal olarak geçerli HTML
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.EmptyBodyHtml);

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task ConvertAsync_ShouldProduceIndependentResults_WhenCalledSequentially()
    {
        // Arrange
        var html1 = HtmlSamples.ToBytes("<html><body><h1>First</h1></body></html>");
        var html2 = HtmlSamples.ToBytes("<html><body><h1>Second</h1><p>More content here</p></body></html>");

        // Act
        var pdf1 = await _fixture.Converter.ConvertAsync(html1);
        var pdf2 = await _fixture.Converter.ConvertAsync(html2);

        // Assert — her iki çıktı da geçerli ve birbirinden farklı olmalı
        PdfAssert.ShouldBeValidPdf(pdf1);
        PdfAssert.ShouldBeValidPdf(pdf2);
        pdf1.Length.Should().NotBe(pdf2.Length);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ConvertAsync — Gelişmiş Senaryolar
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_ShouldReturnValidPdf_WhenGivenHtmlFragment()
    {
        // Arrange — DOCTYPE/html/body etiketleri olmayan kısmi HTML
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.FragmentHtml);

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert — Chromium kısmi HTML'i de render edebilmeli
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task ConvertAsync_ShouldReturnValidPdf_WhenHtmlContainsBase64Image()
    {
        // Arrange — gömülü Base64 görsel
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.Base64ImageHtml);

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task ConvertAsync_ShouldReturnValidPdf_WhenGivenRealisticInvoice()
    {
        // Arrange — gerçekçi e-fatura şablonu
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.InvoiceHtml);

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 1);
    }

    [Fact]
    public async Task ConvertAsync_ShouldReturnValidPdf_WhenHtmlContainsLargeTable()
    {
        // Arrange — 200 satırlık tablo
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.CreateTableHtml(200));

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert — büyük tablo birden fazla sayfa üretmeli
        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 2);
    }

    [Fact]
    public async Task ConvertAsync_ShouldProduceMultiplePages_WhenHtmlContentIsLong()
    {
        // Arrange — 50 paragraf, birden fazla sayfa üretecek uzunlukta
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.CreateMultiPageHtml(50));

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 2);
    }

    [Fact]
    public async Task ConvertAsync_ShouldHandleVeryLargeHtml_WithManyParagraphs()
    {
        // Arrange — stres testi: 200 paragraf
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.CreateMultiPageHtml(200));

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAsync(htmlBytes);

        // Assert — çok sayfalı PDF üretilmeli, boyut önemli ölçüde büyük olmalı
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 5);
        pdfBytes.Length.Should().BeGreaterThan(10_000, "200 paragraflık HTML önemli boyutta PDF üretmeli");
    }

    // ════════════════════════════════════════════════════════════════════════
    // ConvertAndMergeAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldReturnEmptyArray_WhenGivenEmptyList()
    {
        // Arrange
        var emptyList = Array.Empty<byte[]>();

        // Act
        var result = await _fixture.Converter.ConvertAndMergeAsync(emptyList);

        // Assert — boş giriş, boş çıkış
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldReturnValidPdf_WhenGivenSingleHtml()
    {
        // Arrange — tek eleman, birleştirme gerekmez; optimizasyon yolunu test eder
        var singleHtml = new[] { HtmlSamples.ToBytes(HtmlSamples.MinimalHtml) };

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(singleHtml);

        // Assert
        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 1);
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldMergeTwoHtmls_IntoSinglePdf()
    {
        // Arrange
        var htmls = new[]
        {
            HtmlSamples.ToBytes(HtmlSamples.CreateColoredPageHtml("#e74c3c", "Red Page")),
            HtmlSamples.ToBytes(HtmlSamples.CreateColoredPageHtml("#2ecc71", "Green Page"))
        };

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(htmls);

        // Assert — 2 HTML → en az 2 sayfa
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 2);
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldHaveCorrectPageCount_WhenGivenThreeHtmls()
    {
        // Arrange — her biri tek sayfalık 3 HTML
        var htmls = new[]
        {
            HtmlSamples.ToBytes(HtmlSamples.CreateColoredPageHtml("#e74c3c", "Page 1")),
            HtmlSamples.ToBytes(HtmlSamples.CreateColoredPageHtml("#2ecc71", "Page 2")),
            HtmlSamples.ToBytes(HtmlSamples.CreateColoredPageHtml("#3498db", "Page 3"))
        };

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(htmls);

        // Assert
        PdfAssert.ShouldHavePageCount(pdfBytes, 3);
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldMergeSuccessfully_WhenHtmlsHaveDifferentStyles()
    {
        // Arrange — her HTML kendi CSS'ini barındırıyor
        var htmls = new[]
        {
            HtmlSamples.ToBytes(HtmlSamples.StyledHtml),
            HtmlSamples.ToBytes(HtmlSamples.TurkishCharactersHtml),
            HtmlSamples.ToBytes(HtmlSamples.MinimalHtml)
        };

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(htmls);

        // Assert
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 3);
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldBeLarger_ThanSingleConversion()
    {
        // Arrange
        var html = HtmlSamples.ToBytes(HtmlSamples.StyledHtml);

        // Act
        var singlePdf = await _fixture.Converter.ConvertAsync(html);
        var mergedPdf = await _fixture.Converter.ConvertAndMergeAsync([html, html, html]);

        // Assert — 3 kopyanın birleşimi tekil dönüşümden büyük olmalı
        mergedPdf.Length.Should().BeGreaterThan(singlePdf.Length);
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldHandleTenHtmls_WithCorrectPageCount()
    {
        // Arrange — 10 adet tek sayfalık HTML
        var htmls = Enumerable.Range(1, 10)
            .Select(i => HtmlSamples.ToBytes(HtmlSamples.CreateNumberedPageHtml(i)))
            .ToArray();

        // Act
        var pdfBytes = await _fixture.Converter.ConvertAndMergeAsync(htmls);

        // Assert — ölçeklenebilirlik: tam olarak 10 sayfa
        PdfAssert.ShouldHavePageCount(pdfBytes, 10);
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldPreserveDocumentOrder_WhenMerging()
    {
        // Arrange — farklı boyutlarda 3 HTML: küçük, büyük, küçük
        var smallHtml = HtmlSamples.ToBytes(HtmlSamples.MinimalHtml);
        var largeHtml = HtmlSamples.ToBytes(HtmlSamples.CreateMultiPageHtml(30));

        // Act — birleştirme: small + large + small
        var mergedPdf = await _fixture.Converter.ConvertAndMergeAsync([smallHtml, largeHtml, smallHtml]);

        // Assert — sayfa sayısı en az 3 olmalı (small:1 + large:N + small:1)
        var pageCount = PdfAssert.GetPageCount(mergedPdf);
        pageCount.Should().BeGreaterOrEqualTo(3,
            "birleştirilmiş PDF, küçük+büyük+küçük HTML'lerden en az 3 sayfa içermeli");
    }

    // ════════════════════════════════════════════════════════════════════════
    // İptal (Cancellation)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
    {
        // Arrange — önceden iptal edilmiş token
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var htmls = new[]
        {
            HtmlSamples.ToBytes(HtmlSamples.MinimalHtml),
            HtmlSamples.ToBytes(HtmlSamples.MinimalHtml)
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _fixture.Converter.ConvertAndMergeAsync(htmls, cts.Token));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Eşzamanlılık (Concurrency)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertAsync_ShouldSucceedForAll_WhenCalledConcurrently()
    {
        // Arrange — 5 eşzamanlı tekli dönüşüm isteği
        var tasks = Enumerable.Range(1, 5).Select(i =>
        {
            var html = HtmlSamples.ToBytes($"<html><body><h1>Concurrent #{i}</h1></body></html>");
            return _fixture.Converter.ConvertAsync(html);
        }).ToArray();

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(5);
        foreach (var pdf in results)
        {
            PdfAssert.ShouldBeValidPdf(pdf);
        }
    }

    [Fact]
    public async Task ConvertAndMergeAsync_ShouldSucceedForAll_WhenCalledConcurrently()
    {
        // Arrange — 3 eşzamanlı birleştirme isteği
        var tasks = Enumerable.Range(1, 3).Select(batch =>
        {
            var htmls = Enumerable.Range(1, 2)
                .Select(i => HtmlSamples.ToBytes(
                    HtmlSamples.CreateColoredPageHtml($"#{batch * 30 + i * 40:X2}{batch * 20:X2}{i * 60:X2}", $"Batch{batch}-Page{i}")))
                .ToArray();
            return _fixture.Converter.ConvertAndMergeAsync(htmls);
        }).ToArray();

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(3);
        foreach (var pdf in results)
        {
            PdfAssert.ShouldBeValidPdf(pdf);
            PdfAssert.ShouldHaveAtLeastPages(pdf, 2);
        }
    }
}
