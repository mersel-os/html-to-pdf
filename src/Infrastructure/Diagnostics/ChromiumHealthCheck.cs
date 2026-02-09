using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MERSEL.Services.HtmlToPdf.Infrastructure.Diagnostics;

/// <summary>
/// Chromium tarayıcısının bağlı ve çalışır durumda olduğunu doğrulayan sağlık kontrolü.
/// Kubernetes liveness/readiness probe'ları ve yük dengeleyici health check'leri için kullanılır.
/// </summary>
public sealed class ChromiumHealthCheck : IHealthCheck
{
    private readonly PlaywrightPdfConverter _converter;

    public ChromiumHealthCheck(Application.Interfaces.IHtmlToPdfConverter converter)
    {
        // Singleton olan PlaywrightPdfConverter'ı cast et
        _converter = converter as PlaywrightPdfConverter
                     ?? throw new InvalidOperationException(
                         "ChromiumHealthCheck yalnızca PlaywrightPdfConverter ile kullanılabilir");
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var isHealthy = _converter.IsBrowserConnected;

        return Task.FromResult(isHealthy
            ? HealthCheckResult.Healthy("Chromium tarayıcısı bağlı ve hazır")
            : HealthCheckResult.Degraded(
                "Chromium tarayıcısı henüz başlatılmadı. " +
                "İlk istek geldiğinde otomatik başlatılacak."));
    }
}
