# MERSEL.Services.HtmlToPdf.Client

MERSEL HtmlToPdf mikroservisini HTTP üzerinden çağıran **istemci SDK'sı**.

Tek satır DI kaydıyla HTML → PDF dönüşümünü uygulamanıza entegre edin. Servis tamamen stateless çalışır; birden fazla instance ile yatay ölçeklendirme yapabilirsiniz.

## Kurulum

```bash
dotnet add package MERSEL.Services.HtmlToPdf.Client
```

## DI Kaydı

```csharp
// Seçenek 1: appsettings.json'dan URL oku
builder.Services.AddHtmlToPdfClient(builder.Configuration);

// Seçenek 2: Doğrudan URL belirt
builder.Services.AddHtmlToPdfClient("http://htmltopdf-service:8080");
```

**appsettings.json:**

```json
{
  "Services": {
    "HtmlToPdf": {
      "BaseUrl": "http://localhost:5090"
    }
  }
}
```

## Kullanım

```csharp
public class FaturaServisi(IHtmlToPdfConverter pdfConverter)
{
    public async Task<byte[]> FaturaPdfOlustur(string htmlIcerik)
    {
        var htmlBytes = Encoding.UTF8.GetBytes(htmlIcerik);
        return await pdfConverter.ConvertAsync(htmlBytes);
    }

    public async Task<byte[]> TopluRaporOlustur(List<string> htmlSayfalar)
    {
        var htmlByteList = htmlSayfalar.Select(Encoding.UTF8.GetBytes);
        return await pdfConverter.ConvertAndMergeAsync(htmlByteList);
    }
}
```

## Gereksinimler

- .NET 8.0 veya üzeri
- Çalışan bir MERSEL.Services.HtmlToPdf mikroservisi

## Bağlantılar

- [GitHub](https://github.com/mersel-io/MERSEL.Services.HtmlToPdf)
- [Servis Docker Image](https://github.com/mersel-io/MERSEL.Services.HtmlToPdf/pkgs/container/mersel.services.htmltopdf)
- [Arayüz Paketi](https://www.nuget.org/packages/MERSEL.Services.HtmlToPdf.Application)
