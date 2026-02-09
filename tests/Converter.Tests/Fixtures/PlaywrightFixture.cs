using MERSEL.Services.HtmlToPdf.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MERSEL.Services.HtmlToPdf.Converter.Tests.Fixtures;

/// <summary>
/// Playwright tarayıcı örneğini tüm test sınıfları arasında paylaşan xUnit fixture.
/// <see cref="ICollectionFixture{TFixture}"/> ile kullanılır.
/// Tarayıcı ilk test çalıştığında lazy olarak başlatılır, koleksiyondaki
/// tüm testler bittikten sonra kapatılır.
/// Bu sayede her test için tarayıcı başlatma/kapatma maliyeti ortadan kalkar.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    /// <summary>
    /// Paylaşılan PDF dönüştürücü örneği.
    /// Tüm testler bu tek örneği kullanır.
    /// </summary>
    public PlaywrightPdfConverter Converter { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Converter = new PlaywrightPdfConverter(NullLogger<PlaywrightPdfConverter>.Instance);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Converter.DisposeAsync();
    }
}

/// <summary>
/// Playwright tarayıcı paylaşımı için xUnit koleksiyon tanımı.
/// Bu koleksiyonu kullanan tüm test sınıfları aynı <see cref="PlaywrightFixture"/>
/// örneğini paylaşır; böylece tek bir Chromium süreci tüm testlere hizmet eder.
/// </summary>
[CollectionDefinition(Name)]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "Playwright";
}
