using System.Reflection;
using System.Text;

namespace MERSEL.Services.HtmlToPdf.Tests.Shared;

/// <summary>
/// Tüm test projelerinde kullanılan ortak HTML örnek şablonları.
/// Test senaryolarında tekrar kullanımı kolaylaştırır ve tutarlılık sağlar.
/// </summary>
public static class HtmlSamples
{
    // ════════════════════════════════════════════════════════════════════════
    // Gerçek E-Fatura Şablonları (Embedded Resource)
    // ════════════════════════════════════════════════════════════════════════

    private static readonly Lazy<byte[]> _teknolojiFatura = new(() => LoadEmbeddedResource("TeknolojiFatura.html"));
    private static readonly Lazy<byte[]> _temelFatura = new(() => LoadEmbeddedResource("TemelFatura.html"));
    private static readonly Lazy<byte[]> _ticariFatura = new(() => LoadEmbeddedResource("TicariFatura.html"));

    /// <summary>
    /// Gerçek e-fatura şablonu: Teknoloji Destek faturası (~69KB, QR kod JS gömülü).
    /// </summary>
    public static byte[] TeknolojiFaturaBytes => _teknolojiFatura.Value;

    /// <summary>
    /// Gerçek e-fatura şablonu: Temel Fatura (~68KB, QR kod JS gömülü).
    /// </summary>
    public static byte[] TemelFaturaBytes => _temelFatura.Value;

    /// <summary>
    /// Gerçek e-fatura şablonu: Ticari Fatura (~69KB, QR kod JS gömülü).
    /// </summary>
    public static byte[] TicariFaturaBytes => _ticariFatura.Value;

    /// <summary>
    /// Tüm gerçek e-fatura şablonlarını döner (performans testleri için).
    /// </summary>
    public static byte[][] AllRealInvoices =>
    [
        TeknolojiFaturaBytes,
        TemelFaturaBytes,
        TicariFaturaBytes
    ];

    private static byte[] LoadEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MERSEL.Services.HtmlToPdf.Tests.Shared.Samples.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded resource bulunamadı: {resourceName}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// En basit geçerli HTML belgesi.
    /// </summary>
    public const string MinimalHtml = "<html><body><h1>Test</h1></body></html>";

    /// <summary>
    /// DOCTYPE ve html etiketleri olmadan sadece içerik parçası.
    /// Kısmi HTML render dayanıklılığını test eder.
    /// </summary>
    public const string FragmentHtml = "<h1>Fragment Only</h1><p>No doctype, no html tags.</p>";

    /// <summary>
    /// Boş gövdeli, yapısal olarak geçerli HTML.
    /// </summary>
    public const string EmptyBodyHtml = "<html><body></body></html>";

    /// <summary>
    /// Türkçe karakter içeren HTML belgesi.
    /// UTF-8 kodlama doğruluğunu test eder.
    /// </summary>
    public const string TurkishCharactersHtml = """
        <!DOCTYPE html>
        <html lang="tr">
        <head><meta charset="utf-8"><title>Türkçe Test</title></head>
        <body>
            <h1>Türkçe Karakterler</h1>
            <p>ğüşıöçĞÜŞİÖÇ</p>
            <p>Fatura numarası: FTR-2025-001</p>
            <p>Atatürk'ün sözü: "Yurtta sulh, cihanda sulh."</p>
        </body>
        </html>
        """;

    /// <summary>
    /// CSS stilleri ve tablo içeren HTML belgesi.
    /// Stil render doğruluğunu test eder.
    /// </summary>
    public const string StyledHtml = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; }
                h1 { color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #3498db; color: white; }
                tr:nth-child(even) { background-color: #f2f2f2; }
                .total { font-weight: bold; font-size: 1.2em; }
            </style>
        </head>
        <body>
            <h1>Invoice #2025-001</h1>
            <table>
                <tr><th>Product</th><th>Quantity</th><th>Unit Price</th><th>Total</th></tr>
                <tr><td>Software License</td><td>1</td><td>$5,000</td><td>$5,000</td></tr>
                <tr><td>Support Package</td><td>12</td><td>$500</td><td>$6,000</td></tr>
                <tr><td colspan="3" class="total">Grand Total</td><td class="total">$11,000</td></tr>
            </table>
        </body>
        </html>
        """;

    /// <summary>
    /// Gerçekçi e-fatura HTML şablonu.
    /// Firma bilgileri, satır öğeleri, KDV hesabı ve toplam içerir.
    /// Üretim benzeri belge render kalitesini test eder.
    /// </summary>
    public const string InvoiceHtml = """
        <!DOCTYPE html>
        <html lang="tr">
        <head>
            <meta charset="utf-8">
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { font-family: 'Segoe UI', Tahoma, sans-serif; padding: 40px; color: #333; }
                .header { display: flex; justify-content: space-between; margin-bottom: 40px; }
                .company { font-size: 24px; font-weight: bold; color: #2c3e50; }
                .invoice-info { text-align: right; }
                .invoice-no { font-size: 20px; color: #e74c3c; }
                table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                th { background: #34495e; color: white; padding: 12px 8px; text-align: left; }
                td { padding: 10px 8px; border-bottom: 1px solid #eee; }
                .amount { text-align: right; }
                .totals { margin-top: 20px; text-align: right; }
                .totals .grand { font-size: 1.4em; color: #e74c3c; font-weight: bold; }
                .footer { margin-top: 60px; padding-top: 20px; border-top: 2px solid #eee; font-size: 0.85em; color: #777; }
            </style>
        </head>
        <body>
            <div class="header">
                <div>
                    <div class="company">MERSEL YAZILIM A.Ş.</div>
                    <p>Levent Mah. Teknoloji Cad. No:42</p>
                    <p>34330 Beşiktaş / İstanbul</p>
                    <p>VKN: 1234567890</p>
                </div>
                <div class="invoice-info">
                    <div class="invoice-no">FATURA #FTR-2025-00142</div>
                    <p>Tarih: 15.01.2025</p>
                    <p>Vade: 15.02.2025</p>
                </div>
            </div>
            <table>
                <thead>
                    <tr><th>#</th><th>Ürün/Hizmet</th><th>Miktar</th><th class="amount">Birim Fiyat</th><th class="amount">Tutar</th></tr>
                </thead>
                <tbody>
                    <tr><td>1</td><td>E-Fatura Yazılım Lisansı (Yıllık)</td><td>1</td><td class="amount">₺45.000,00</td><td class="amount">₺45.000,00</td></tr>
                    <tr><td>2</td><td>Teknik Destek Paketi (12 Ay)</td><td>12</td><td class="amount">₺3.500,00</td><td class="amount">₺42.000,00</td></tr>
                    <tr><td>3</td><td>Entegrasyon Hizmeti</td><td>1</td><td class="amount">₺15.000,00</td><td class="amount">₺15.000,00</td></tr>
                    <tr><td>4</td><td>Eğitim (Uzaktan, 8 Saat)</td><td>2</td><td class="amount">₺5.000,00</td><td class="amount">₺10.000,00</td></tr>
                </tbody>
            </table>
            <div class="totals">
                <p>Ara Toplam: ₺112.000,00</p>
                <p>KDV (%20): ₺22.400,00</p>
                <p class="grand">Genel Toplam: ₺134.400,00</p>
            </div>
            <div class="footer">
                <p>Bu fatura elektronik ortamda oluşturulmuştur. GİB onaylıdır.</p>
                <p>MERSEL YAZILIM A.Ş. — ETTN: a1b2c3d4-e5f6-7890-abcd-ef1234567890</p>
            </div>
        </body>
        </html>
        """;

    /// <summary>
    /// Base64 kodlu gömülü görsel içeren HTML.
    /// Gömülü kaynakların render edilebilirliğini test eder.
    /// </summary>
    public const string Base64ImageHtml = """
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Image Test</title></head>
        <body>
            <h1>Embedded Image</h1>
            <img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAFklEQVQYV2P8z8BQz0AEYBxVOHIUAgBGWAgE/dLkWAAAAABJRU5ErkJggg==" 
                 alt="Red pixel" width="100" height="100" />
            <p>A small red square image rendered from base64.</p>
        </body>
        </html>
        """;

    /// <summary>
    /// Çok sayfalı bir HTML belgesi oluşturur.
    /// <paramref name="paragraphCount"/> kadar paragraf ekleyerek sayfa sınırını aştırır.
    /// </summary>
    public static string CreateMultiPageHtml(int paragraphCount = 50)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>");
        for (var i = 1; i <= paragraphCount; i++)
        {
            sb.AppendLine($"<h2>Section {i}</h2>");
            sb.AppendLine("<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                          "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                          "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.</p>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Renkli arka planlı tek sayfalık HTML oluşturur.
    /// Birleştirme testlerinde sayfa ayrımı için kullanılır.
    /// </summary>
    public static string CreateColoredPageHtml(string color, string title) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                body { background-color: {{color}}; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }
                h1 { font-size: 3em; color: white; text-shadow: 2px 2px 4px rgba(0,0,0,0.5); }
            </style>
        </head>
        <body>
            <h1>{{title}}</h1>
        </body>
        </html>
        """;

    /// <summary>
    /// Belirtilen satır sayısında tablo içeren HTML oluşturur.
    /// Büyük tablo render performansını test eder.
    /// </summary>
    public static string CreateTableHtml(int rowCount = 100)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8">
            <style>
                table { width: 100%; border-collapse: collapse; font-size: 11px; }
                th, td { border: 1px solid #ccc; padding: 4px 8px; }
                th { background: #34495e; color: white; }
                tr:nth-child(even) { background: #f9f9f9; }
            </style>
            </head><body>
            <h1>Data Table Report</h1>
            <table>
            <thead><tr><th>#</th><th>Code</th><th>Description</th><th>Amount</th><th>Date</th></tr></thead>
            <tbody>
            """);

        for (var i = 1; i <= rowCount; i++)
        {
            sb.AppendLine(
                $"<tr><td>{i}</td><td>ITM-{i:D5}</td><td>Item description for row {i}</td><td>${i * 42.50m:N2}</td><td>2025-01-{(i % 28) + 1:D2}</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Numaralı sayfa oluşturur (sıralama doğrulaması için).
    /// Her sayfa ortada büyük bir sayfa numarası içerir.
    /// </summary>
    public static string CreateNumberedPageHtml(int pageNumber) => $$"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8">
        <style>
            body { display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; background: #f0f0f0; }
            .number { font-size: 120px; font-weight: bold; color: #2c3e50; }
        </style>
        </head>
        <body><div class="number">{{pageNumber}}</div></body>
        </html>
        """;

    /// <summary>
    /// HTML metnini UTF-8 byte dizisine dönüştürür.
    /// </summary>
    public static byte[] ToBytes(string html) => Encoding.UTF8.GetBytes(html);
}
