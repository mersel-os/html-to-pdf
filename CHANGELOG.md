# Changelog

Bu proje [Semantic Versioning](https://semver.org/lang/tr/) kullanır.

## [1.1.0] - 2026-02-09

### Eklenenler

- **OpenTelemetry Metrikleri**: Dönüşüm sayısı, süre, boyut ve hata oranı metrikleri (`System.Diagnostics.Metrics`)
- **Prometheus Endpoint**: `/metrics` endpoint'i ile Prometheus-uyumlu metrik dışa aktarımı
- **Dağıtık İzleme (Tracing)**: Her dönüşüm işlemi için ActivitySource span'ları
- **Chromium Sağlık Kontrolü**: ASP.NET Core HealthCheck altyapısı ile Chromium bağlantı durumu
- **GitHub Actions CI/CD**: Otomatik build, test, NuGet yayınlama ve Docker image push
- **NuGet Paket Altyapısı**: `MERSEL.Services.HtmlToPdf.Client` ve `Application` paketleri
- **GitHub Release Otomasyonu**: Tag push ile otomatik release notes ve paket eki

### İyileştirmeler

- Sağlık kontrolü artık Chromium tarayıcısının bağlantı durumunu da raporlar
- ASP.NET Core HTTP istek ve .NET Runtime metrikleri otomatik toplanır
- `AddHtmlToPdfInfrastructure()` artık `IMeterFactory` kaydını otomatik yapar

## [1.0.0] - 2025-02-08

### Eklenenler

- **Tek HTML → PDF dönüşümü**: `POST /convert` endpoint'i
- **Çoklu HTML → Birleştirilmiş PDF**: `POST /convert/merge` endpoint'i
- **Playwright (Chromium) tabanlı render**: Tam CSS/JS desteği ile piksel mükemmelliğinde PDF çıktısı
- **PdfSharpCore ile PDF birleştirme**: Birden fazla HTML'i tek PDF'te birleştirme
- **İstemci SDK'sı**: .NET tüketicileri için `HtmlToPdfClient` HTTP istemcisi
- **DI uzantı metodları**: `AddHtmlToPdfInfrastructure()` ve `AddHtmlToPdfClient()`
- **Otomatik Chromium yükleme**: Uygulama başlangıcında tarayıcı kontrolü ve indirme
- **Docker desteği**: Üretim hazır Dockerfile
- **Serilog entegrasyonu**: Yapılandırılmış loglama (Console + Seq)
- **OpenAPI/Scalar desteği**: Etkileşimli API dokümantasyonu
- **Sağlık kontrolü**: `GET /health` endpoint'i
- **Kapsamlı test paketi**: 50+ birim ve entegrasyon testi
