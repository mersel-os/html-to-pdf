using MERSEL.Services.HtmlToPdf.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MERSEL.Services.HtmlToPdf.Client;

/// <summary>
/// HtmlToPdf İstemci SDK'sı için DI uzantı metodları.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Uzak HtmlToPdf mikroservisini çağıran HTTP istemcisini DI'ye kaydeder.
    /// Servis URL'sini <c>Services:HtmlToPdf:BaseUrl</c> yapılandırma anahtarından okur.
    /// </summary>
    /// <example>
    /// <code>
    /// // appsettings.json
    /// {
    ///   "Services": {
    ///     "HtmlToPdf": {
    ///       "BaseUrl": "http://localhost:5090"
    ///     }
    ///   }
    /// }
    ///
    /// // Program.cs
    /// builder.Services.AddHtmlToPdfClient(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddHtmlToPdfClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Services:HtmlToPdf:BaseUrl"]
                      ?? "http://localhost:5090";

        services.AddHttpClient("HtmlToPdf", client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddTransient<IHtmlToPdfConverter, HtmlToPdfClient>();

        return services;
    }

    /// <summary>
    /// Uzak HtmlToPdf mikroservisini çağıran HTTP istemcisini
    /// doğrudan URL belirterek DI'ye kaydeder.
    /// </summary>
    /// <param name="services">Servis koleksiyonu</param>
    /// <param name="baseUrl">HtmlToPdf mikroservisinin temel URL'si</param>
    public static IServiceCollection AddHtmlToPdfClient(
        this IServiceCollection services,
        string baseUrl)
    {
        services.AddHttpClient("HtmlToPdf", client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddTransient<IHtmlToPdfConverter, HtmlToPdfClient>();

        return services;
    }
}
