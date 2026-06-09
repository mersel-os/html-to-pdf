using System.Diagnostics;
using System.Globalization;
using MERSEL.Services.HtmlToPdf.Application.Interfaces;
using MERSEL.Services.HtmlToPdf.Application.Models;
using MERSEL.Services.HtmlToPdf.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace MERSEL.Services.HtmlToPdf.Infrastructure;

/// <summary>
/// Playwright (Chromium) tabanlı HTML → PDF dönüşüm servisi.
/// Tarayıcı örneği uygulama ömrü boyunca tekil tutulur (Singleton).
/// Her dönüşüm izolasyon için yeni bir tarayıcı bağlamı (context) oluşturur.
/// </summary>
public sealed class PlaywrightPdfConverter : IHtmlToPdfConverter, IAsyncDisposable
{
    private readonly ILogger<PlaywrightPdfConverter> _logger;
    private readonly HtmlToPdfMetrics? _metrics;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private static bool _browserInstalled;

    public PlaywrightPdfConverter(ILogger<PlaywrightPdfConverter> logger, HtmlToPdfMetrics? metrics = null)
    {
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// Chromium tarayıcısının bağlı ve hazır olup olmadığını döner.
    /// Sağlık kontrolü (health check) tarafından kullanılır.
    /// </summary>
    public bool IsBrowserConnected => _browser is { IsConnected: true };

    private const string AutoInstallEnvVar = "PLAYWRIGHT_AUTO_INSTALL";

    /// <summary>
    /// Chromium tarayıcısının yüklü olup olmadığını kontrol eder,
    /// yüklü değilse otomatik olarak indirir.
    /// Uygulama başlangıcında (Program.cs) çağrılması önerilir; fail-fast sağlar.
    /// Zaten yüklüyse hiçbir şey yapmaz (idempotent).
    /// <para>
    /// <c>PLAYWRIGHT_AUTO_INSTALL</c> ortam değişkeni ile kontrol edilir:
    /// <list type="bullet">
    ///   <item><c>true</c> (varsayılan): Chromium yoksa otomatik indirir</item>
    ///   <item><c>false</c>: İndirmeyi atlar, tarayıcının önceden yüklenmiş olduğunu varsayar</item>
    /// </list>
    /// </para>
    /// </summary>
    public static void EnsureBrowserInstalled(ILogger? logger = null)
    {
        if (_browserInstalled)
            return;

        var autoInstall = Environment.GetEnvironmentVariable(AutoInstallEnvVar) is not "false";

        if (!autoInstall)
        {
            logger?.LogInformation("Playwright otomatik kurulum devre dışı (PLAYWRIGHT_AUTO_INSTALL=false)");
            _browserInstalled = true;
            return;
        }

        logger?.LogInformation("Playwright Chromium tarayıcısı kontrol ediliyor...");

        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);

        if (exitCode != 0)
            throw new InvalidOperationException(
                $"Playwright Chromium yüklemesi başarısız oldu (çıkış kodu: {exitCode}). " +
                "Lütfen ağ bağlantısını ve disk alanını kontrol edin.");

        _browserInstalled = true;
        logger?.LogInformation("Playwright Chromium tarayıcısı hazır");
    }

    /// <inheritdoc />
    public Task<byte[]> ConvertAsync(byte[] htmlContent, CancellationToken ct = default)
        => ConvertAsync(htmlContent, new PdfConvertOptions(), ct);

    /// <inheritdoc />
    public async Task<byte[]> ConvertAsync(byte[] htmlContent, PdfConvertOptions options, CancellationToken ct = default)
    {
        using var activity = HtmlToPdfMetrics.ActivitySource.StartActivity("ConvertSingle");
        _metrics?.IncrementActive();
        var sw = Stopwatch.StartNew();

        try
        {
            var pdfOptions = BuildPdfOptions(options);
            var browser = await GetBrowserAsync();
            await using var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await LoadHtmlAsync(page, htmlContent, options);

            if (options.SmartShrinking)
                await ApplySmartShrinkingAsync(page, options, pdfOptions);

            var pdfBytes = await page.PdfAsync(pdfOptions);
            await page.CloseAsync();

            sw.Stop();
            _metrics?.RecordConversion("single", sw.Elapsed.TotalMilliseconds, pdfBytes.Length);
            activity?.SetTag("pdf.size", pdfBytes.Length);

            _logger.LogDebug("Tekli dönüşüm tamamlandı: {Sure}ms, {Boyut} byte",
                sw.Elapsed.TotalMilliseconds.ToString("F1"), pdfBytes.Length);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _metrics?.RecordError("single");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Tekli PDF dönüşümü başarısız oldu ({Sure}ms)", sw.Elapsed.TotalMilliseconds.ToString("F1"));
            throw;
        }
        finally
        {
            _metrics?.DecrementActive();
        }
    }

    /// <inheritdoc />
    public Task<byte[]> ConvertAndMergeAsync(IEnumerable<byte[]> htmlContents, CancellationToken ct = default)
        => ConvertAndMergeAsync(htmlContents, new PdfConvertOptions(), ct);

    /// <inheritdoc />
    public async Task<byte[]> ConvertAndMergeAsync(IEnumerable<byte[]> htmlContents, PdfConvertOptions options, CancellationToken ct = default)
    {
        var htmlList = htmlContents.ToList();

        if (htmlList.Count == 0)
            return [];

        if (htmlList.Count == 1)
            return await ConvertAsync(htmlList[0], options, ct);

        using var activity = HtmlToPdfMetrics.ActivitySource.StartActivity("ConvertAndMerge");
        activity?.SetTag("html.count", htmlList.Count);
        _metrics?.IncrementActive();
        var sw = Stopwatch.StartNew();

        try
        {
            var pdfOptions = BuildPdfOptions(options);
            var pdfBytesList = new List<byte[]>(htmlList.Count);
            var browser = await GetBrowserAsync();
            await using var context = await browser.NewContextAsync();

            foreach (var htmlContent in htmlList)
            {
                ct.ThrowIfCancellationRequested();

                var page = await context.NewPageAsync();
                await LoadHtmlAsync(page, htmlContent, options);

                // Her belge farklı genişlikte olabileceği için her seferinde ölçülür
                if (options.SmartShrinking)
                {
                    var pagePdfOptions = BuildPdfOptions(options);
                    await ApplySmartShrinkingAsync(page, options, pagePdfOptions);
                    pdfBytesList.Add(await page.PdfAsync(pagePdfOptions));
                }
                else
                {
                    pdfBytesList.Add(await page.PdfAsync(pdfOptions));
                }

                await page.CloseAsync();
            }

            var mergedPdf = MergePdfs(pdfBytesList);

            sw.Stop();
            _metrics?.RecordConversion("merge", sw.Elapsed.TotalMilliseconds, mergedPdf.Length);
            activity?.SetTag("pdf.size", mergedPdf.Length);

            _logger.LogDebug("Birleştirme dönüşümü tamamlandı: {Adet} HTML, {Sure}ms, {Boyut} byte",
                htmlList.Count, sw.Elapsed.TotalMilliseconds.ToString("F1"), mergedPdf.Length);

            return mergedPdf;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _metrics?.RecordError("merge");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "PDF birleştirme dönüşümü başarısız oldu ({Adet} HTML, {Sure}ms)",
                htmlList.Count, sw.Elapsed.TotalMilliseconds.ToString("F1"));
            throw;
        }
        finally
        {
            _metrics?.DecrementActive();
        }
    }

    // ── HTML Yükleme (JavaScript delay + network idle desteği) ──────

    /// <summary>
    /// HTML içeriğini sayfaya yükler. Options'a göre ek bekleme uygular:
    /// - WaitForNetworkIdle: tüm ağ istekleri tamamlanana kadar bekler
    /// - JavaScriptDelayMs: ek sabit bekleme süresi uygular
    /// </summary>
    private static async Task LoadHtmlAsync(IPage page, byte[] htmlContent, PdfConvertOptions options)
    {
        var html = System.Text.Encoding.UTF8.GetString(htmlContent);

        // WaitForNetworkIdle ise "networkidle" bekleme stratejisi kullan
        var waitUntil = options.WaitForNetworkIdle
            ? WaitUntilState.NetworkIdle
            : WaitUntilState.Load;

        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = waitUntil });

        // Ek JavaScript bekleme süresi (wkhtmltopdf'in --javascript-delay karşılığı)
        if (options.JavaScriptDelayMs is > 0)
        {
            await page.WaitForTimeoutAsync(options.JavaScriptDelayMs.Value);
        }
    }

    // ── PdfConvertOptions → Playwright PagePdfOptions Dönüşümü ──────

    /// <summary>
    /// Application katmanının PdfConvertOptions modelini Playwright'ın PagePdfOptions'ına dönüştürür.
    /// </summary>
    private static PagePdfOptions BuildPdfOptions(PdfConvertOptions options)
    {
        var pdfOptions = new PagePdfOptions
        {
            Landscape = options.Landscape,
            Scale = options.Scale,
            PrintBackground = options.PrintBackground,
            PreferCSSPageSize = options.PreferCssPageSize,
            DisplayHeaderFooter = options.DisplayHeaderFooter,
            HeaderTemplate = options.HeaderTemplate,
            FooterTemplate = options.FooterTemplate,
            PageRanges = options.PageRanges,
            Margin = new Margin
            {
                Top = options.MarginTop,
                Bottom = options.MarginBottom,
                Left = options.MarginLeft,
                Right = options.MarginRight
            }
        };

        // Özel genişlik/yükseklik belirtildiyse Format yerine kullan
        if (!string.IsNullOrEmpty(options.Width) || !string.IsNullOrEmpty(options.Height))
        {
            pdfOptions.Width = options.Width;
            pdfOptions.Height = options.Height;
        }
        else
        {
            pdfOptions.Format = options.Format;
        }

        return pdfOptions;
    }

    // ── PDF Birleştirme ─────────────────────────────────────────────

    /// <summary>
    /// Birden fazla PDF byte dizisini tek bir PDF belgesinde birleştirir.
    /// </summary>
    private static byte[] MergePdfs(List<byte[]> pdfBytesList)
    {
        using var outputDocument = new PdfDocument();

        foreach (var pdfBytes in pdfBytesList)
        {
            using var stream = new MemoryStream(pdfBytes);
            using var inputDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

            for (var i = 0; i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
            }
        }

        using var outputStream = new MemoryStream();
        outputDocument.Save(outputStream, false);
        return outputStream.ToArray();
    }

    // ── Akıllı Küçültme (Smart Shrinking) ──────────────────────────

    /// <summary>
    /// wkhtmltopdf'in <c>--enable-smart-shrinking</c> davranışını uygular.
    /// <list type="number">
    ///   <item>Kağıdın yazdırılabilir genişliğini (mm → CSS px) hesaplar</item>
    ///   <item>Viewport'u bu genişliğe ayarlar (doğru ölçüm için)</item>
    ///   <item>İçeriğin gerçek genişliğini <c>scrollWidth</c> ile ölçer</item>
    ///   <item>İçerik taşıyorsa <c>scale</c> değerini otomatik hesaplar</item>
    /// </list>
    /// </summary>
    private async Task ApplySmartShrinkingAsync(IPage page, PdfConvertOptions options, PagePdfOptions pdfOptions)
    {
        var paperWidthMm = GetPaperWidthMm(options);
        var marginLeftMm = ParseCssUnitToMm(options.MarginLeft);
        var marginRightMm = ParseCssUnitToMm(options.MarginRight);
        var printableWidthMm = paperWidthMm - marginLeftMm - marginRightMm;

        // CSS pixel cinsine çevir (1in = 25.4mm, 1in = 96px)
        var printableWidthPx = printableWidthMm / 25.4 * 96.0;

        // Viewport'u yazdırılabilir genişliğe ayarla (içeriğin bu genişlikte nasıl davranacağını ölçmek için)
        await page.SetViewportSizeAsync((int)Math.Ceiling(printableWidthPx), 1080);

        // Layout yeniden hesaplansın
        await page.EvaluateAsync("() => document.body.offsetHeight");

        // İçeriğin gerçek genişliğini ölç
        var contentWidth = await page.EvaluateAsync<double>(
            "() => Math.max(document.body.scrollWidth, document.documentElement.scrollWidth)");

        if (contentWidth > printableWidthPx + 1) // +1 yuvarlama toleransı
        {
            var autoScale = (float)(printableWidthPx / contentWidth);

            // Kullanıcının belirlediği scale ile çarpışmasın; daha küçük olanı al
            autoScale = Math.Min(autoScale, options.Scale);
            autoScale = Math.Clamp(autoScale, 0.1f, 2.0f);

            pdfOptions.Scale = autoScale;

            _logger.LogDebug(
                "Smart shrinking uygulandı: içerik={IcerikPx:F0}px, yazdırılabilir={YazPx:F0}px, ölçek={Olcek:F3}",
                contentWidth, printableWidthPx, autoScale);
        }
        else
        {
            _logger.LogDebug(
                "Smart shrinking: içerik sayfaya sığıyor ({IcerikPx:F0}px ≤ {YazPx:F0}px), ölçekleme gerekmedi",
                contentWidth, printableWidthPx);
        }
    }

    // ── Kağıt Boyutu Tablosu ─────────────────────────────────────────

    /// <summary>
    /// Standart kağıt boyutları (genişlik × yükseklik, mm cinsinden, dikey yönde).
    /// </summary>
    private static readonly Dictionary<string, (double WidthMm, double HeightMm)> PaperSizes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["A0"] = (841, 1189),
            ["A1"] = (594, 841),
            ["A2"] = (420, 594),
            ["A3"] = (297, 420),
            ["A4"] = (210, 297),
            ["A5"] = (148, 210),
            ["A6"] = (105, 148),
            ["Letter"] = (215.9, 279.4),
            ["Legal"] = (215.9, 355.6),
            ["Tabloid"] = (279.4, 431.8),
            ["Ledger"] = (431.8, 279.4),
        };

    /// <summary>
    /// Options'tan kağıdın genişliğini mm olarak hesaplar.
    /// Yatay yönde (Landscape) genişlik ve yükseklik yer değiştirir.
    /// Özel Width belirtilmişse onu kullanır.
    /// </summary>
    private static double GetPaperWidthMm(PdfConvertOptions options)
    {
        if (!string.IsNullOrEmpty(options.Width))
            return ParseCssUnitToMm(options.Width);

        if (PaperSizes.TryGetValue(options.Format, out var size))
            return options.Landscape ? size.HeightMm : size.WidthMm;

        // Bilinmeyen format — A4 varsayılan
        return options.Landscape ? 297 : 210;
    }

    /// <summary>
    /// CSS birimini (mm, cm, in, px) milimetreye çevirir.
    /// Desteklenen formatlar: "10mm", "1cm", "0.5in", "20px", "10" (mm varsayılır).
    /// </summary>
    private static double ParseCssUnitToMm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        value = value.Trim().ToLowerInvariant();

        if (value.EndsWith("mm") && double.TryParse(value[..^2], CultureInfo.InvariantCulture, out var mm))
            return mm;

        if (value.EndsWith("cm") && double.TryParse(value[..^2], CultureInfo.InvariantCulture, out var cm))
            return cm * 10;

        if (value.EndsWith("in") && double.TryParse(value[..^2], CultureInfo.InvariantCulture, out var inch))
            return inch * 25.4;

        if (value.EndsWith("px") && double.TryParse(value[..^2], CultureInfo.InvariantCulture, out var px))
            return px / 96.0 * 25.4;

        // Birimsiz sayı — mm varsay
        if (double.TryParse(value, CultureInfo.InvariantCulture, out var num))
            return num;

        return 0;
    }

    // ── Tarayıcı Yönetimi ───────────────────────────────────────────

    /// <summary>
    /// Chromium tarayıcı örneğini getirir. İlk çağrıda lazy olarak başlatılır.
    /// Double-check locking ile thread-safe'dir.
    /// </summary>
    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsConnected: true })
            return _browser;

        await _initLock.WaitAsync();
        try
        {
            if (_browser is { IsConnected: true })
                return _browser;

            EnsureBrowserInstalled(_logger);

            _logger.LogInformation("Playwright Chromium tarayıcısı başlatılıyor...");

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-gpu", "--disable-dev-shm-usage"]
            });

            _logger.LogInformation("Playwright Chromium tarayıcısı başarıyla başlatıldı");
            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Tarayıcı ve Playwright kaynaklarını serbest bırakır.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
        _initLock.Dispose();
    }
}
