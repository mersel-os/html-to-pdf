# MERSEL.Services.HtmlToPdf.Application

MERSEL HtmlToPdf mikroservisinin **temel sözleşme (contract) katmanı**.

Bu paket, HTML → PDF dönüşüm servisinin `IHtmlToPdfConverter` arayüzünü içerir. Kendi implementasyonunuzu yazmak veya istemci SDK'sını (`MERSEL.Services.HtmlToPdf.Client`) kullanmak için bu paketi referans alın.

## Arayüz

```csharp
public interface IHtmlToPdfConverter
{
    // Tek HTML → PDF
    Task<byte[]> ConvertAsync(byte[] htmlContent, CancellationToken ct = default);

    // Çoklu HTML → Birleştirilmiş PDF
    Task<byte[]> ConvertAndMergeAsync(IEnumerable<byte[]> htmlContents, CancellationToken ct = default);
}
```

## Kullanım

Çoğu senaryoda doğrudan bu paketi değil, hazır HTTP istemcisi içeren **MERSEL.Services.HtmlToPdf.Client** paketini kullanmanız önerilir:

```bash
dotnet add package MERSEL.Services.HtmlToPdf.Client
```

Bu paketi yalnızca şu durumlarda doğrudan referans alın:
- Kendi `IHtmlToPdfConverter` implementasyonunuzu yazmak istiyorsanız
- Servis sözleşmesine sadece arayüz seviyesinde bağımlılık istiyorsanız

## Bağlantılar

- [GitHub](https://github.com/mersel-io/MERSEL.Services.HtmlToPdf)
- [İstemci SDK Paketi](https://www.nuget.org/packages/MERSEL.Services.HtmlToPdf.Client)
