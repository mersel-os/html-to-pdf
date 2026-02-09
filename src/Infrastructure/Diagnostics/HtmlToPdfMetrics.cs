using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MERSEL.Services.HtmlToPdf.Infrastructure.Diagnostics;

/// <summary>
/// HtmlToPdf servisinin tüm özel metriklerini tanımlar.
/// System.Diagnostics.Metrics altyapısını kullanır — OpenTelemetry, Prometheus
/// veya herhangi bir .NET metrikleri ile uyumludur.
/// </summary>
public sealed class HtmlToPdfMetrics
{
    /// <summary>
    /// Meter adı. OpenTelemetry yapılandırmasında <c>AddMeter(HtmlToPdfMetrics.MeterName)</c>
    /// ile bu metrikler dışarıya aktarılır.
    /// </summary>
    public const string MeterName = "MERSEL.Services.HtmlToPdf";

    /// <summary>
    /// Dağıtık izleme (distributed tracing) için ActivitySource.
    /// Her dönüşüm işlemi ayrı bir Activity (span) olarak kaydedilir.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(MeterName);

    private readonly Counter<long> _conversionsTotal;
    private readonly Counter<long> _conversionErrors;
    private readonly Histogram<double> _conversionDuration;
    private readonly Histogram<long> _pdfSize;
    private readonly UpDownCounter<int> _activeConversions;

    public HtmlToPdfMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _conversionsTotal = meter.CreateCounter<long>(
            "htmltopdf.conversions.total",
            description: "Toplam başarılı PDF dönüşüm sayısı");

        _conversionErrors = meter.CreateCounter<long>(
            "htmltopdf.conversions.errors",
            description: "Başarısız dönüşüm sayısı");

        _conversionDuration = meter.CreateHistogram<double>(
            "htmltopdf.conversion.duration",
            unit: "ms",
            description: "PDF dönüşüm süresi (milisaniye)");

        _pdfSize = meter.CreateHistogram<long>(
            "htmltopdf.pdf.size",
            unit: "By",
            description: "Oluşturulan PDF boyutu (byte)");

        _activeConversions = meter.CreateUpDownCounter<int>(
            "htmltopdf.conversions.active",
            description: "Devam eden aktif dönüşüm sayısı");
    }

    /// <summary>
    /// Başarılı bir dönüşümü kaydeder.
    /// </summary>
    /// <param name="type">Dönüşüm tipi: "single" veya "merge"</param>
    /// <param name="durationMs">İşlem süresi (milisaniye)</param>
    /// <param name="pdfSizeBytes">Oluşturulan PDF boyutu (byte)</param>
    public void RecordConversion(string type, double durationMs, long pdfSizeBytes)
    {
        var tag = new KeyValuePair<string, object?>("type", type);
        _conversionsTotal.Add(1, tag);
        _conversionDuration.Record(durationMs, tag);
        _pdfSize.Record(pdfSizeBytes, tag);
    }

    /// <summary>
    /// Başarısız bir dönüşümü kaydeder.
    /// </summary>
    /// <param name="type">Dönüşüm tipi: "single" veya "merge"</param>
    public void RecordError(string type)
    {
        _conversionErrors.Add(1, new KeyValuePair<string, object?>("type", type));
    }

    /// <summary>Aktif dönüşüm sayacını artırır.</summary>
    public void IncrementActive() => _activeConversions.Add(1);

    /// <summary>Aktif dönüşüm sayacını azaltır.</summary>
    public void DecrementActive() => _activeConversions.Add(-1);
}
