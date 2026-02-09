using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Infrastructure;
using MERSEL.Services.HtmlToPdf.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MERSEL.Services.HtmlToPdf.Converter.Tests;

/// <summary>
/// PlaywrightPdfConverter yaşam döngüsü testleri.
/// Dispose güvenliği, idempotent davranış ve ilk kullanım senaryolarını kapsar.
/// Bu testler paylaşılan Playwright koleksiyonunu KULLANMAZ;
/// her test kendi converter örneğini oluşturur.
/// </summary>
public class ConverterLifecycleTests
{
    [Fact]
    public async Task DisposeAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange — dönüştürücüyü oluştur ve bir kez kullan
        var converter = new PlaywrightPdfConverter(NullLogger<PlaywrightPdfConverter>.Instance);
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.MinimalHtml);
        await converter.ConvertAsync(htmlBytes);

        // Act — Dispose'u art arda 3 kez çağır
        var act = async () =>
        {
            await converter.DisposeAsync();
            await converter.DisposeAsync();
            await converter.DisposeAsync();
        };

        // Assert — hiçbir çağrı hata fırlatmamalı
        await act.Should().NotThrowAsync(
            "DisposeAsync birden çok kez çağrılabilmeli (idempotent)");
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow_WhenConverterWasNeverUsed()
    {
        // Arrange — dönüştürücüyü oluştur ama hiç kullanma
        var converter = new PlaywrightPdfConverter(NullLogger<PlaywrightPdfConverter>.Instance);

        // Act & Assert — hiç kullanılmadan dispose edilebilmeli
        var act = async () => await converter.DisposeAsync();
        await act.Should().NotThrowAsync(
            "tarayıcı hiç başlatılmadıysa dispose güvenli olmalı");
    }

    [Fact]
    public void EnsureBrowserInstalled_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Act — aynı metodu art arda çağır
        var act = () =>
        {
            PlaywrightPdfConverter.EnsureBrowserInstalled();
            PlaywrightPdfConverter.EnsureBrowserInstalled();
            PlaywrightPdfConverter.EnsureBrowserInstalled();
        };

        // Assert — her çağrı güvenli olmalı (zaten yüklüyse atlar)
        act.Should().NotThrow(
            "EnsureBrowserInstalled tekrarlayan çağrılarda hata vermemeli (idempotent)");
    }

    [Fact]
    public async Task ConvertAsync_ShouldWorkCorrectly_WithFreshConverterInstance()
    {
        // Arrange — her şey sıfırdan: yeni instance, ilk kullanım
        await using var converter = new PlaywrightPdfConverter(NullLogger<PlaywrightPdfConverter>.Instance);
        var htmlBytes = HtmlSamples.ToBytes(HtmlSamples.InvoiceHtml);

        // Act
        var pdfBytes = await converter.ConvertAsync(htmlBytes);

        // Assert — ilk kullanımda da doğru çalışmalı
        PdfAssert.ShouldBeValidPdf(pdfBytes);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 1);
    }
}
