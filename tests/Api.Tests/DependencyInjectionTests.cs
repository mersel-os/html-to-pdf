using FluentAssertions;
using MERSEL.Services.HtmlToPdf.Client;
using MERSEL.Services.HtmlToPdf.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AppIHtmlToPdfConverter = MERSEL.Services.HtmlToPdf.Application.Interfaces.IHtmlToPdfConverter;
using ClientIHtmlToPdfConverter = MERSEL.Services.HtmlToPdf.Client.Interfaces.IHtmlToPdfConverter;

namespace MERSEL.Services.HtmlToPdf.Api.Tests;

/// <summary>
/// DI kayıt uzantı metodlarının doğruluk testleri.
/// AddHtmlToPdfInfrastructure ve AddHtmlToPdfClient uzantılarının
/// beklenen tipleri ve yaşam döngülerini doğru kaydedip kaydetmediğini doğrular.
/// </summary>
public class DependencyInjectionTests
{
    // ════════════════════════════════════════════════════════════════════════
    // AddHtmlToPdfInfrastructure (Sunucu tarafı)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AddHtmlToPdfInfrastructure_ShouldRegisterConverter_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddHtmlToPdfInfrastructure();
        var provider = services.BuildServiceProvider();

        // Assert — IHtmlToPdfConverter çözümlenebilmeli (Application interface — sunucu tarafı)
        var converter = provider.GetService<AppIHtmlToPdfConverter>();
        converter.Should().NotBeNull();
        converter.Should().BeOfType<PlaywrightPdfConverter>();

        // Singleton doğrulaması: iki kez çözümle, aynı instance olmalı
        var converter2 = provider.GetService<AppIHtmlToPdfConverter>();
        converter.Should().BeSameAs(converter2);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AddHtmlToPdfClient (İstemci tarafı)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AddHtmlToPdfClient_WithConfiguration_ShouldRegisterClient()
    {
        // Arrange — yapılandırmadan URL oku
        var services = new ServiceCollection();
        services.AddLogging();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Services:HtmlToPdf:BaseUrl"] = "http://test-server:5090"
            })
            .Build();

        // Act
        services.AddHtmlToPdfClient(config);
        var provider = services.BuildServiceProvider();

        // Assert — IHtmlToPdfConverter, HtmlToPdfClient olarak çözülmeli (Client interface)
        var converter = provider.GetService<ClientIHtmlToPdfConverter>();
        converter.Should().NotBeNull();
        converter.Should().BeOfType<HtmlToPdfClient>();
    }

    [Fact]
    public void AddHtmlToPdfClient_WithExplicitUrl_ShouldRegisterClient()
    {
        // Arrange — doğrudan URL belirt
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddHtmlToPdfClient("http://custom-url:8080");
        var provider = services.BuildServiceProvider();

        // Assert
        var converter = provider.GetService<ClientIHtmlToPdfConverter>();
        converter.Should().NotBeNull();
        converter.Should().BeOfType<HtmlToPdfClient>();
    }

    [Fact]
    public void AddHtmlToPdfClient_ShouldRegisterAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHtmlToPdfClient("http://localhost:5090");

        var provider = services.BuildServiceProvider();

        // Act — iki ayrı scope'ta çözümle
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var instance1 = scope1.ServiceProvider.GetService<ClientIHtmlToPdfConverter>();
        var instance2 = scope2.ServiceProvider.GetService<ClientIHtmlToPdfConverter>();

        // Assert — Transient kayıt: her seferinde farklı instance
        instance1.Should().NotBeSameAs(instance2);
    }

    [Fact]
    public void AddHtmlToPdfClient_WithConfiguration_ShouldFallbackToDefaultUrl_WhenKeyIsMissing()
    {
        // Arrange — yapılandırmada URL yok, varsayılan kullanılmalı
        var services = new ServiceCollection();
        services.AddLogging();

        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddHtmlToPdfClient(emptyConfig);
        var provider = services.BuildServiceProvider();

        // Assert — hata vermeden çözümlenmeli (Client interface)
        var converter = provider.GetService<ClientIHtmlToPdfConverter>();
        converter.Should().NotBeNull();
    }
}
