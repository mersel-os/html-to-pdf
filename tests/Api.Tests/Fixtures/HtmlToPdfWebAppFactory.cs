using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MERSEL.Services.HtmlToPdf.Api.Tests.Fixtures;

/// <summary>
/// HtmlToPdf Web API'si için test sunucu fabrikası.
/// WebApplicationFactory, gerçek bir test HTTP sunucusu ayağa kaldırır.
/// PlaywrightPdfConverter Singleton olarak çalışır, tüm testlerde paylaşılır.
/// </summary>
public class HtmlToPdfWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
