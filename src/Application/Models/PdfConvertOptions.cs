namespace MERSEL.Services.HtmlToPdf.Application.Models;

/// <summary>
/// PDF dönüşüm ayarları.
/// wkhtmltopdf'in komut satırı parametrelerinin Playwright (Chromium) karşılıklarını içerir.
/// Tüm alanlar opsiyoneldir — belirtilmezse varsayılan değerler kullanılır.
/// </summary>
/// <remarks>
/// <b>wkhtmltopdf → Playwright Eşleştirmesi:</b>
/// <list type="table">
///   <listheader>
///     <term>wkhtmltopdf Parametresi</term>
///     <description>PdfConvertOptions Property</description>
///   </listheader>
///   <item><term>--page-size A4</term><description><see cref="Format"/></description></item>
///   <item><term>--orientation Landscape</term><description><see cref="Landscape"/></description></item>
///   <item><term>--enable-smart-shrinking</term><description><see cref="Scale"/> (0.75-0.85 arası önerilir)</description></item>
///   <item><term>--lowquality</term><description><see cref="Scale"/> (düşük değer = küçük dosya)</description></item>
///   <item><term>--javascript-delay N</term><description><see cref="JavaScriptDelayMs"/></description></item>
///   <item><term>--no-stop-slow-scripts</term><description><see cref="WaitForNetworkIdle"/></description></item>
///   <item><term>--margin-top/bottom/left/right</term><description><see cref="MarginTop"/>, <see cref="MarginBottom"/>, <see cref="MarginLeft"/>, <see cref="MarginRight"/></description></item>
///   <item><term>--header-html / --footer-html</term><description><see cref="HeaderTemplate"/>, <see cref="FooterTemplate"/></description></item>
///   <item><term>--header-spacing / --footer-spacing</term><description>Margin değerleri ile kontrol edilir</description></item>
/// </list>
/// </remarks>
public class PdfConvertOptions
{
    /// <summary>
    /// Kağıt boyutu formatı.
    /// Desteklenen değerler: Letter, Legal, Tabloid, Ledger, A0, A1, A2, A3, A4, A5, A6.
    /// <para>wkhtmltopdf karşılığı: <c>--page-size</c></para>
    /// </summary>
    public string Format { get; set; } = "A4";

    /// <summary>
    /// Yatay sayfa yönü. <c>true</c> ise Landscape, <c>false</c> ise Portrait.
    /// <para>wkhtmltopdf karşılığı: <c>--orientation Landscape</c></para>
    /// </summary>
    public bool Landscape { get; set; }

    /// <summary>
    /// Render ölçek faktörü. 0.1 ile 2.0 arasında bir değer.
    /// <para>
    /// wkhtmltopdf'in <c>--enable-smart-shrinking</c> davranışını karşılamak için
    /// 0.75-0.85 arası değer önerilir. <c>--lowquality</c> karşılığı olarak
    /// düşük scale değeri daha küçük dosya boyutu üretir.
    /// </para>
    /// <para>Varsayılan: 1.0 (orijinal boyut)</para>
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>
    /// Arka plan renkleri ve resimlerini PDF'e dahil et.
    /// <c>false</c> ise sadece metin ve ön plan öğeleri yazdırılır.
    /// <para>Varsayılan: <c>true</c></para>
    /// </summary>
    public bool PrintBackground { get; set; } = true;

    // ── Kenar Boşlukları (CSS birimleriyle: "10mm", "1cm", "0.5in", "20px") ──

    /// <summary>
    /// Üst kenar boşluğu. Header kullanılıyorsa artırılmalıdır.
    /// <para>wkhtmltopdf karşılığı: <c>--margin-top</c></para>
    /// <para>Varsayılan: "10mm"</para>
    /// </summary>
    public string MarginTop { get; set; } = "10mm";

    /// <summary>
    /// Alt kenar boşluğu. Footer kullanılıyorsa artırılmalıdır.
    /// <para>wkhtmltopdf karşılığı: <c>--margin-bottom</c></para>
    /// <para>Varsayılan: "10mm"</para>
    /// </summary>
    public string MarginBottom { get; set; } = "10mm";

    /// <summary>
    /// Sol kenar boşluğu.
    /// <para>wkhtmltopdf karşılığı: <c>--margin-left</c></para>
    /// <para>Varsayılan: "5mm"</para>
    /// </summary>
    public string MarginLeft { get; set; } = "5mm";

    /// <summary>
    /// Sağ kenar boşluğu.
    /// <para>wkhtmltopdf karşılığı: <c>--margin-right</c></para>
    /// <para>Varsayılan: "5mm"</para>
    /// </summary>
    public string MarginRight { get; set; } = "5mm";

    // ── Header / Footer Şablonları ────────────────────────────────────

    /// <summary>
    /// Header ve footer şablonlarını görüntüle.
    /// <see cref="HeaderTemplate"/> veya <see cref="FooterTemplate"/> kullanılıyorsa
    /// <c>true</c> olmalıdır.
    /// </summary>
    public bool DisplayHeaderFooter { get; set; }

    /// <summary>
    /// Sayfa başlığı (header) HTML şablonu.
    /// Chromium özel CSS sınıflarını destekler:
    /// <list type="bullet">
    ///   <item><c>date</c> — biçimlendirilmiş yazdırma tarihi</item>
    ///   <item><c>title</c> — belge başlığı</item>
    ///   <item><c>url</c> — belge URL'si</item>
    ///   <item><c>pageNumber</c> — geçerli sayfa numarası</item>
    ///   <item><c>totalPages</c> — toplam sayfa sayısı</item>
    /// </list>
    /// <para>
    /// Örnek: <c>&lt;div style="font-size:10px; text-align:center; width:100%;"&gt;
    /// Sayfa &lt;span class="pageNumber"&gt;&lt;/span&gt; / &lt;span class="totalPages"&gt;&lt;/span&gt;
    /// &lt;/div&gt;</c>
    /// </para>
    /// <para>wkhtmltopdf karşılığı: <c>--header-html</c> (inline HTML olarak)</para>
    /// </summary>
    public string? HeaderTemplate { get; set; }

    /// <summary>
    /// Sayfa alt bilgisi (footer) HTML şablonu.
    /// <see cref="HeaderTemplate"/> ile aynı özel CSS sınıflarını destekler.
    /// <para>wkhtmltopdf karşılığı: <c>--footer-html</c> (inline HTML olarak)</para>
    /// </summary>
    public string? FooterTemplate { get; set; }

    // ── JavaScript / Render Bekleme ───────────────────────────────────

    /// <summary>
    /// HTML yüklendikten sonra JavaScript'in çalışması için beklenecek ek süre (milisaniye).
    /// Dinamik içerik, grafik kütüphaneleri veya JavaScript ile render edilen tablolar için kullanılır.
    /// <para>wkhtmltopdf karşılığı: <c>--javascript-delay</c></para>
    /// <para>Varsayılan: null (ek bekleme yok)</para>
    /// </summary>
    public int? JavaScriptDelayMs { get; set; }

    /// <summary>
    /// Tüm ağ istekleri tamamlanana kadar bekle (500ms boyunca yeni istek olmayana dek).
    /// Harici kaynak (font, resim, API) yükleyen sayfalar için faydalıdır.
    /// <para>wkhtmltopdf karşılığı: <c>--no-stop-slow-scripts</c> ile benzer etki</para>
    /// <para>Varsayılan: <c>false</c></para>
    /// </summary>
    public bool WaitForNetworkIdle { get; set; }

    // ── Akıllı Küçültme ──────────────────────────────────────────────

    /// <summary>
    /// Akıllı küçültme (Smart Shrinking). <c>true</c> ise içerik genişliği ölçülür
    /// ve kağıt genişliğinden taşıyorsa otomatik olarak <see cref="Scale"/> hesaplanır.
    /// <para>
    /// wkhtmltopdf'in <c>--enable-smart-shrinking</c> davranışının birebir karşılığıdır.
    /// İçerik sayfaya zaten sığıyorsa herhangi bir ölçekleme yapılmaz.
    /// </para>
    /// <para>Varsayılan: <c>false</c></para>
    /// </summary>
    public bool SmartShrinking { get; set; }

    // ── Diğer Ayarlar ─────────────────────────────────────────────────

    /// <summary>
    /// HTML'deki CSS <c>@page</c> boyutunu tercih et.
    /// <c>true</c> ise HTML'de tanımlanan sayfa boyutu <see cref="Format"/> yerine kullanılır.
    /// <para>Varsayılan: <c>false</c></para>
    /// </summary>
    public bool PreferCssPageSize { get; set; }

    /// <summary>
    /// Yazdırılacak sayfa aralıkları. Örnek: "1-5", "1,3,5-9".
    /// <c>null</c> ise tüm sayfalar dahil edilir.
    /// </summary>
    public string? PageRanges { get; set; }

    /// <summary>
    /// Özel sayfa genişliği (CSS birimi). Belirtilirse <see cref="Format"/> yerine kullanılır.
    /// Örnek: "210mm", "8.5in", "800px"
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// Özel sayfa yüksekliği (CSS birimi). Belirtilirse <see cref="Format"/> yerine kullanılır.
    /// Örnek: "297mm", "11in", "1200px"
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// wkhtmltopdf'in varsayılan <c>--enable-smart-shrinking</c> davranışını
    /// birebir karşılayan preset.
    /// <see cref="SmartShrinking"/> etkin — içerik sayfaya sığmazsa otomatik küçültülür.
    /// </summary>
    public static PdfConvertOptions WkHtmlCompatible => new()
    {
        Format = "A4",
        Landscape = false,
        SmartShrinking = true,
        PrintBackground = true,
        MarginTop = "5mm",
        MarginBottom = "5mm",
        MarginLeft = "0mm",
        MarginRight = "1mm",
        WaitForNetworkIdle = true,
        JavaScriptDelayMs = 2000
    };

    /// <summary>
    /// wkhtmltopdf'in birleştirilmiş PDF (unified) ayarlarını karşılayan preset.
    /// <see cref="SmartShrinking"/> etkin, daha yüksek JavaScript bekleme süresi.
    /// </summary>
    public static PdfConvertOptions WkHtmlMergeCompatible => new()
    {
        Format = "A4",
        Landscape = false,
        SmartShrinking = true,
        PrintBackground = true,
        MarginTop = "5mm",
        MarginBottom = "5mm",
        MarginLeft = "0mm",
        MarginRight = "1mm",
        WaitForNetworkIdle = true,
        JavaScriptDelayMs = 5000
    };
}
