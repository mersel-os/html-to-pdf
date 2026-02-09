using System.Net;
using System.Text;
using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Api.Tests.Fixtures;
using MERSEL.Services.HtmlToPdf.Tests.Shared;
using Xunit;

namespace MERSEL.Services.HtmlToPdf.Api.Tests;

/// <summary>
/// POST /convert endpoint testleri.
/// Tek HTML belgesini PDF'e dönüştürme senaryolarını kapsar.
/// Tüm istekler multipart/form-data ile "file" alanından gönderilir.
/// </summary>
public class ConvertEndpointTests : IClassFixture<HtmlToPdfWebAppFactory>
{
    private readonly HttpClient _client;

    public ConvertEndpointTests(HtmlToPdfWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>Multipart form-data content oluşturan yardımcı metot.</summary>
    private static MultipartFormDataContent CreateFormContent(byte[] htmlBytes, string fileName = "document.html")
    {
        var formContent = new MultipartFormDataContent();
        var filePart = new ByteArrayContent(htmlBytes);
        filePart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
        formContent.Add(filePart, "file", fileName);
        return formContent;
    }

    [Fact]
    public async Task PostConvert_ShouldReturnPdf_WhenGivenValidHtml()
    {
        // Arrange
        using var content = CreateFormContent(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        // Act
        var response = await _client.PostAsync("/convert", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task PostConvert_ShouldReturnBadRequest_WhenFileIsEmpty()
    {
        // Arrange — boş dosya gönder
        using var content = CreateFormContent([]);

        // Act
        var response = await _client.PostAsync("/convert", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostConvert_ShouldReturnValidPdf_WhenGivenStyledHtml()
    {
        // Arrange — CSS stilleri içeren HTML
        using var content = CreateFormContent(HtmlSamples.ToBytes(HtmlSamples.StyledHtml));

        // Act
        var response = await _client.PostAsync("/convert", content);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task PostConvert_ShouldReturnValidPdf_WhenGivenTurkishHtml()
    {
        // Arrange — Türkçe karakter içeren HTML
        using var content = CreateFormContent(HtmlSamples.ToBytes(HtmlSamples.TurkishCharactersHtml));

        // Act
        var response = await _client.PostAsync("/convert", content);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task PostConvert_ShouldReturnValidPdf_WhenGivenRealisticInvoice()
    {
        // Arrange — gerçekçi e-fatura şablonu
        using var content = CreateFormContent(HtmlSamples.ToBytes(HtmlSamples.InvoiceHtml));

        // Act
        var response = await _client.PostAsync("/convert", content);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PdfAssert.ShouldBeValidPdf(pdfBytes);
    }

    [Fact]
    public async Task PostConvert_ShouldReturnPdfWithMultiplePages_WhenHtmlIsLong()
    {
        // Arrange — sayfa sınırını aşacak uzunlukta HTML
        var longHtml = new StringBuilder();
        longHtml.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>");
        for (var i = 0; i < 50; i++)
            longHtml.Append(
                $"<h2>Section {i}</h2><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor.</p>");
        longHtml.Append("</body></html>");

        using var content = CreateFormContent(Encoding.UTF8.GetBytes(longHtml.ToString()));

        // Act
        var response = await _client.PostAsync("/convert", content);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PdfAssert.ShouldHaveAtLeastPages(pdfBytes, 2);
    }

    [Fact]
    public async Task PostConvert_ShouldSetCorrectContentDisposition()
    {
        // Arrange
        using var content = CreateFormContent(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        // Act
        var response = await _client.PostAsync("/convert", content);

        // Assert — dosya adı "document.pdf" olarak dönmeli
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition?.FileName.Should().Be("document.pdf");
    }

    [Fact]
    public async Task PostConvert_ShouldReturnApplicationPdfContentType()
    {
        // Arrange
        using var content = CreateFormContent(HtmlSamples.ToBytes(HtmlSamples.MinimalHtml));

        // Act
        var response = await _client.PostAsync("/convert", content);

        // Assert — Content-Type kesinlikle application/pdf olmalı
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
    }
}
