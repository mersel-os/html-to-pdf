using System.Net;
using System.Text.Json;
using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Api.Tests.Fixtures;
using Xunit;

namespace MERSEL.Services.HtmlToPdf.Api.Tests;

/// <summary>
/// /health endpoint testleri.
/// Servisin çalışır durumda olduğunu doğrular.
/// </summary>
public class HealthCheckTests : IClassFixture<HtmlToPdfWebAppFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(HtmlToPdfWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnValidStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert — yanıt gövdesinde geçerli durum ve servis adı olmalı
        // Chromium lazy-init olduğu için ilk istekte "Degraded" dönebilir,
        // PDF dönüşümü yapıldıktan sonra "Healthy" olur
        var status = json.RootElement.GetProperty("status").GetString();
        status.Should().BeOneOf("Healthy", "Degraded");
        json.RootElement.GetProperty("service").GetString().Should().Be("HtmlToPdf");
        json.RootElement.GetProperty("checks").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthy_AfterConverterIsUsed()
    {
        // Arrange — Chromium'u başlatmak için bir dönüşüm yap
        using var formContent = new MultipartFormDataContent();
        var filePart = new ByteArrayContent("<html><body>test</body></html>"u8.ToArray());
        filePart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
        formContent.Add(filePart, "file", "test.html");
        await _client.PostAsync("/convert", formContent);

        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert — Chromium başlatıldıktan sonra "Healthy" olmalı
        json.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_ShouldReturnJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
