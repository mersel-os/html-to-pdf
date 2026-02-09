using MERSEL.Services.HtmlToPdf.Application.Interfaces;
using MERSEL.Services.HtmlToPdf.Infrastructure.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace MERSEL.Services.HtmlToPdf.Infrastructure;

/// <summary>
/// HtmlToPdf Infrastructure katmanı DI kayıt uzantıları.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Playwright tabanlı PDF dönüştürücüyü ve ilgili servisleri DI'ye kaydeder.
    /// <list type="bullet">
    ///   <item>PlaywrightPdfConverter — Singleton (tarayıcı uygulama ömrü boyunca ayakta kalır)</item>
    ///   <item>HtmlToPdfMetrics — Singleton (metrik sayaçları paylaşılır)</item>
    ///   <item>ChromiumHealthCheck — Health check kaydı</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddHtmlToPdfInfrastructure(this IServiceCollection services)
    {
        // System.Diagnostics.Metrics — IMeterFactory DI kaydı
        services.AddMetrics();

        // Özel HtmlToPdf metrikleri
        services.AddSingleton<HtmlToPdfMetrics>();

        // PDF dönüştürücü (Singleton — tarayıcı uygulama ömrü boyunca)
        services.AddSingleton<PlaywrightPdfConverter>();
        services.AddSingleton<IHtmlToPdfConverter>(sp => sp.GetRequiredService<PlaywrightPdfConverter>());

        // Sağlık kontrolü (Chromium bağlantı durumu)
        services.AddHealthChecks()
            .AddCheck<ChromiumHealthCheck>("chromium", tags: ["ready"]);

        return services;
    }
}
