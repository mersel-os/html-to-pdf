using System.Text;
using FluentAssertions;
using PdfSharpCore.Pdf.IO;

namespace MERSEL.Services.HtmlToPdf.Tests.Shared;

/// <summary>
/// PDF doğrulama yardımcı metodları.
/// Tüm test projelerinde tutarlı PDF assertion'ları sağlar.
/// Tekrar eden IsValidPdf / GetPageCount gibi yardımcıları merkezileştirir.
/// </summary>
public static class PdfAssert
{
    /// <summary>
    /// Byte dizisinin geçerli bir PDF dosyası olup olmadığını kontrol eder.
    /// PDF dosyaları "%PDF-" magic bytes ile başlar.
    /// </summary>
    public static bool IsValidPdf(byte[] pdfBytes)
    {
        if (pdfBytes.Length < 5)
            return false;

        var header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        return header == "%PDF-";
    }

    /// <summary>
    /// PdfSharpCore kullanarak PDF'in sayfa sayısını döndürür.
    /// </summary>
    public static int GetPageCount(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return document.PageCount;
    }

    /// <summary>
    /// Byte dizisinin boş olmadığını ve geçerli bir PDF olduğunu doğrular.
    /// Başarısız olursa açıklayıcı hata mesajı verir.
    /// </summary>
    public static void ShouldBeValidPdf(byte[] pdfBytes)
    {
        pdfBytes.Should().NotBeNullOrEmpty("PDF çıktısı boş olmamalı");
        IsValidPdf(pdfBytes).Should().BeTrue("byte dizisi geçerli bir PDF başlığı (%PDF-) içermeli");
    }

    /// <summary>
    /// PDF'in tam olarak belirtilen sayfa sayısına sahip olduğunu doğrular.
    /// Önce geçerli bir PDF olup olmadığını kontrol eder.
    /// </summary>
    public static void ShouldHavePageCount(byte[] pdfBytes, int expectedPageCount)
    {
        ShouldBeValidPdf(pdfBytes);
        GetPageCount(pdfBytes).Should().Be(expectedPageCount,
            $"PDF tam olarak {expectedPageCount} sayfa içermeli");
    }

    /// <summary>
    /// PDF'in en az belirtilen sayfa sayısına sahip olduğunu doğrular.
    /// </summary>
    public static void ShouldHaveAtLeastPages(byte[] pdfBytes, int minimumPageCount)
    {
        ShouldBeValidPdf(pdfBytes);
        GetPageCount(pdfBytes).Should().BeGreaterOrEqualTo(minimumPageCount,
            $"PDF en az {minimumPageCount} sayfa içermeli");
    }
}
