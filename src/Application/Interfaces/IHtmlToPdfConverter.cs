using MERSEL.Services.HtmlToPdf.Application.Models;

namespace MERSEL.Services.HtmlToPdf.Application.Interfaces;

/// <summary>
/// HTML içeriğini PDF belgesine dönüştüren servis arayüzü.
/// HtmlToPdf mikroservisinin temel sözleşmesidir.
/// Tüketiciler bu arayüzü uzaktan HTTP istemcisi (<c>HtmlToPdfClient</c>) ile
/// veya süreç içi Playwright implementasyonu (<c>PlaywrightPdfConverter</c>) ile kullanabilir.
/// </summary>
public interface IHtmlToPdfConverter
{
    /// <summary>
    /// Tek bir HTML belgesini varsayılan ayarlarla PDF'e dönüştürür.
    /// </summary>
    /// <param name="htmlContent">HTML içeriği (UTF-8 kodlu byte dizisi)</param>
    /// <param name="ct">İptal belirteci</param>
    /// <returns>PDF belgesi (byte dizisi)</returns>
    Task<byte[]> ConvertAsync(byte[] htmlContent, CancellationToken ct = default);

    /// <summary>
    /// Tek bir HTML belgesini belirtilen ayarlarla PDF'e dönüştürür.
    /// </summary>
    /// <param name="htmlContent">HTML içeriği (UTF-8 kodlu byte dizisi)</param>
    /// <param name="options">PDF dönüşüm ayarları (sayfa boyutu, yön, ölçek, kenar boşlukları vb.)</param>
    /// <param name="ct">İptal belirteci</param>
    /// <returns>PDF belgesi (byte dizisi)</returns>
    Task<byte[]> ConvertAsync(byte[] htmlContent, PdfConvertOptions options, CancellationToken ct = default);

    /// <summary>
    /// Birden fazla HTML belgesini varsayılan ayarlarla tek bir birleştirilmiş PDF'e dönüştürür.
    /// </summary>
    /// <param name="htmlContents">HTML içerikleri koleksiyonu (byte dizileri)</param>
    /// <param name="ct">İptal belirteci</param>
    /// <returns>Birleştirilmiş PDF belgesi (byte dizisi)</returns>
    Task<byte[]> ConvertAndMergeAsync(IEnumerable<byte[]> htmlContents, CancellationToken ct = default);

    /// <summary>
    /// Birden fazla HTML belgesini belirtilen ayarlarla tek bir birleştirilmiş PDF'e dönüştürür.
    /// Her HTML bağımsız olarak kendi head/style/body'si ile render edilir,
    /// ardından tüm render edilen sayfalar tek PDF dosyasında birleştirilir.
    /// </summary>
    /// <param name="htmlContents">HTML içerikleri koleksiyonu (byte dizileri)</param>
    /// <param name="options">PDF dönüşüm ayarları (tüm belgelere uygulanır)</param>
    /// <param name="ct">İptal belirteci</param>
    /// <returns>Birleştirilmiş PDF belgesi (byte dizisi)</returns>
    Task<byte[]> ConvertAndMergeAsync(IEnumerable<byte[]> htmlContents, PdfConvertOptions options, CancellationToken ct = default);
}
