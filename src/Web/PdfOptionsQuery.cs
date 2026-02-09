using MERSEL.Services.HtmlToPdf.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace MERSEL.Services.HtmlToPdf.Web;

/// <summary>
/// API query string'den gelen PDF ayarlarını bağlayan model.
/// <c>[AsParameters]</c> ile kullanılır; her property query string'den okunur.
/// Belirtilmeyen (null) alanlar varsayılan değerlerle doldurulur.
/// </summary>
public class PdfOptionsQuery
{
    /// <summary>Kağıt boyutu: A4, A3, Letter, Legal, Tabloid vb.</summary>
    [FromQuery(Name = "format")]
    public string? Format { get; set; }

    /// <summary>Yatay sayfa yönü</summary>
    [FromQuery(Name = "landscape")]
    public bool? Landscape { get; set; }

    /// <summary>Render ölçek faktörü (0.1 - 2.0)</summary>
    [FromQuery(Name = "scale")]
    public float? Scale { get; set; }

    /// <summary>Arka plan renkleri/resimleri yazdır</summary>
    [FromQuery(Name = "printBackground")]
    public bool? PrintBackground { get; set; }

    /// <summary>Üst kenar boşluğu (ör: "10mm", "1cm")</summary>
    [FromQuery(Name = "marginTop")]
    public string? MarginTop { get; set; }

    /// <summary>Alt kenar boşluğu</summary>
    [FromQuery(Name = "marginBottom")]
    public string? MarginBottom { get; set; }

    /// <summary>Sol kenar boşluğu</summary>
    [FromQuery(Name = "marginLeft")]
    public string? MarginLeft { get; set; }

    /// <summary>Sağ kenar boşluğu</summary>
    [FromQuery(Name = "marginRight")]
    public string? MarginRight { get; set; }

    /// <summary>Header/Footer görüntüle</summary>
    [FromQuery(Name = "displayHeaderFooter")]
    public bool? DisplayHeaderFooter { get; set; }

    /// <summary>Header HTML şablonu</summary>
    [FromQuery(Name = "headerTemplate")]
    public string? HeaderTemplate { get; set; }

    /// <summary>Footer HTML şablonu</summary>
    [FromQuery(Name = "footerTemplate")]
    public string? FooterTemplate { get; set; }

    /// <summary>JavaScript bekleme süresi (ms)</summary>
    [FromQuery(Name = "jsDelay")]
    public int? JsDelay { get; set; }

    /// <summary>Ağ istekleri tamamlanana kadar bekle</summary>
    [FromQuery(Name = "waitForNetworkIdle")]
    public bool? WaitForNetworkIdle { get; set; }

    /// <summary>CSS @page boyutunu tercih et</summary>
    [FromQuery(Name = "preferCssPageSize")]
    public bool? PreferCssPageSize { get; set; }

    /// <summary>Sayfa aralıkları (ör: "1-5", "1,3,5-9")</summary>
    [FromQuery(Name = "pageRanges")]
    public string? PageRanges { get; set; }

    /// <summary>Özel sayfa genişliği (ör: "210mm")</summary>
    [FromQuery(Name = "width")]
    public string? Width { get; set; }

    /// <summary>Özel sayfa yüksekliği (ör: "297mm")</summary>
    [FromQuery(Name = "height")]
    public string? Height { get; set; }

    /// <summary>
    /// Akıllı küçültme — içerik sayfaya sığmazsa otomatik ölçekle.
    /// wkhtmltopdf'in --enable-smart-shrinking karşılığıdır.
    /// </summary>
    [FromQuery(Name = "smartShrinking")]
    public bool? SmartShrinking { get; set; }

    /// <summary>
    /// Query parametrelerini <see cref="PdfConvertOptions"/> modeline dönüştürür.
    /// Null olan alanlar varsayılan değerlerde bırakılır.
    /// </summary>
    public PdfConvertOptions ToOptions()
    {
        var options = new PdfConvertOptions();

        if (!string.IsNullOrEmpty(Format)) options.Format = Format;
        if (Landscape.HasValue) options.Landscape = Landscape.Value;
        if (Scale.HasValue) options.Scale = Scale.Value;
        if (PrintBackground.HasValue) options.PrintBackground = PrintBackground.Value;
        if (!string.IsNullOrEmpty(MarginTop)) options.MarginTop = MarginTop;
        if (!string.IsNullOrEmpty(MarginBottom)) options.MarginBottom = MarginBottom;
        if (!string.IsNullOrEmpty(MarginLeft)) options.MarginLeft = MarginLeft;
        if (!string.IsNullOrEmpty(MarginRight)) options.MarginRight = MarginRight;
        if (DisplayHeaderFooter.HasValue) options.DisplayHeaderFooter = DisplayHeaderFooter.Value;
        if (!string.IsNullOrEmpty(HeaderTemplate)) options.HeaderTemplate = HeaderTemplate;
        if (!string.IsNullOrEmpty(FooterTemplate)) options.FooterTemplate = FooterTemplate;
        if (JsDelay.HasValue) options.JavaScriptDelayMs = JsDelay.Value;
        if (WaitForNetworkIdle.HasValue) options.WaitForNetworkIdle = WaitForNetworkIdle.Value;
        if (PreferCssPageSize.HasValue) options.PreferCssPageSize = PreferCssPageSize.Value;
        if (!string.IsNullOrEmpty(PageRanges)) options.PageRanges = PageRanges;
        if (!string.IsNullOrEmpty(Width)) options.Width = Width;
        if (!string.IsNullOrEmpty(Height)) options.Height = Height;
        if (SmartShrinking.HasValue) options.SmartShrinking = SmartShrinking.Value;

        return options;
    }
}
