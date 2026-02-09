using System.Net;
using System.Text;
using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Api.Tests.Fixtures;
using MERSEL.Services.HtmlToPdf.Tests.Shared;
using Xunit;

namespace MERSEL.Services.HtmlToPdf.Api.Tests;

/// <summary>
/// POST /convert/merge endpoint testleri.
/// Birden fazla HTML'i tek bir birleştirilmiş PDF'e dönüştürme senaryolarını kapsar.
/// </summary>
public class ConvertMergeEndpointTests : IClassFixture<HtmlToPdfWebAppFactory>
{
    private readonly HttpClient _client;

    public ConvertMergeEndpointTests(HtmlToPdfWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostConvertMerge_ShouldReturnMergedPdf_WhenGivenMultipleFiles()
    {
        // Arrange — 2 farklı renkli sayfa gönder
        using var formContent = new MultipartFormDataContent();
        AddHtmlFile(formContent, HtmlSamples.CreateColoredPageHtml("#e74c3c", "Red"), "file0", "page1.html");
        AddHtmlFile(formContent, HtmlSamples.CreateColoredPageHtml("#2ecc71", "Green"), "file1", "page2.html");

        // Act
        var response = await _client.PostAsync("/convert/merge", formContent);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        // Assert — 2 HTML'den en az 2 sayfa
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 2);
    }

    [Fact]
    public async Task PostConvertMerge_ShouldReturnBadRequest_WhenOnlyEmptyFieldSent()
    {
        // Arrange — dosya yerine sadece metin alanı gönder (Files koleksiyonu boş kalır)
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent("no-file"), "description");

        // Act
        var response = await _client.PostAsync("/convert/merge", formContent);

        // Assert — dosya yoksa 400 dönmeli
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostConvertMerge_ShouldReturnValidPdf_WhenGivenSingleFile()
    {
        // Arrange — tek dosya: birleştirmeye gerek yok, yine de çalışmalı
        using var formContent = new MultipartFormDataContent();
        AddHtmlFile(formContent, HtmlSamples.MinimalHtml, "file0", "single.html");

        // Act
        var response = await _client.PostAsync("/convert/merge", formContent);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task PostConvertMerge_ShouldHaveCorrectPageCount_WhenGivenThreeFiles()
    {
        // Arrange — her biri tek sayfalık 3 HTML dosyası
        using var formContent = new MultipartFormDataContent();
        AddHtmlFile(formContent, HtmlSamples.CreateColoredPageHtml("#e74c3c", "P1"), "file0", "p1.html");
        AddHtmlFile(formContent, HtmlSamples.CreateColoredPageHtml("#2ecc71", "P2"), "file1", "p2.html");
        AddHtmlFile(formContent, HtmlSamples.CreateColoredPageHtml("#3498db", "P3"), "file2", "p3.html");

        // Act
        var response = await _client.PostAsync("/convert/merge", formContent);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PdfAssert.ShouldHavePageCount(pdfBytes, 3);
    }

    [Fact]
    public async Task PostConvertMerge_ShouldHandleTenFiles_WithCorrectPageCount()
    {
        // Arrange — ölçeklenebilirlik: 10 adet tek sayfalık HTML
        using var formContent = new MultipartFormDataContent();
        for (var i = 0; i < 10; i++)
        {
            AddHtmlFile(formContent,
                HtmlSamples.CreateNumberedPageHtml(i + 1),
                $"file{i}",
                $"page{i + 1}.html");
        }

        // Act
        var response = await _client.PostAsync("/convert/merge", formContent);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        // Assert — tam olarak 10 sayfa
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PdfAssert.ShouldHavePageCount(pdfBytes, 10);
    }

    [Fact]
    public async Task PostConvertMerge_ShouldSetCorrectContentDisposition()
    {
        // Arrange
        using var formContent = new MultipartFormDataContent();
        AddHtmlFile(formContent, HtmlSamples.MinimalHtml, "file0", "page.html");
        AddHtmlFile(formContent, HtmlSamples.MinimalHtml, "file1", "page2.html");

        // Act
        var response = await _client.PostAsync("/convert/merge", formContent);

        // Assert — birleşik dosya adı "merged.pdf" olmalı
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition?.FileName.Should().Be("merged.pdf");
    }

    [Fact]
    public async Task PostConvertMerge_ShouldProduceLargerPdf_ThanSingleConversion()
    {
        // Arrange — aynı HTML'i hem tek hem 3 kopya olarak gönder
        var singleContent = new ByteArrayContent(HtmlSamples.ToBytes(HtmlSamples.StyledHtml));
        singleContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");

        using var mergeContent = new MultipartFormDataContent();
        AddHtmlFile(mergeContent, HtmlSamples.StyledHtml, "file0", "p1.html");
        AddHtmlFile(mergeContent, HtmlSamples.StyledHtml, "file1", "p2.html");
        AddHtmlFile(mergeContent, HtmlSamples.StyledHtml, "file2", "p3.html");

        // Act
        var singleResponse = await _client.PostAsync("/convert", singleContent);
        var mergeResponse = await _client.PostAsync("/convert/merge", mergeContent);

        var singlePdf = await singleResponse.Content.ReadAsByteArrayAsync();
        var mergedPdf = await mergeResponse.Content.ReadAsByteArrayAsync();

        // Assert — birleşik PDF tek PDF'den büyük olmalı
        mergedPdf.Length.Should().BeGreaterThan(singlePdf.Length);
    }

    [Fact]
    public async Task PostConvertMerge_ShouldReturnApplicationPdfContentType()
    {
        // Arrange
        using var formContent = new MultipartFormDataContent();
        AddHtmlFile(formContent, HtmlSamples.MinimalHtml, "file0", "page.html");

        // Act
        var response = await _client.PostAsync("/convert/merge", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
    }

    // ────────────────────────────────────────────────────────────────────

    private static void AddHtmlFile(MultipartFormDataContent form, string html, string name, string fileName)
    {
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes(html));
        bytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
        form.Add(bytes, name, fileName);
    }
}
