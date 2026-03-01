using AiLogoMaker.Application.Services;
using AiLogoMaker.Domain.Services;
using AiLogoMaker.Domain.Services.Export;
using Microsoft.Extensions.DependencyInjection;

namespace AiLogoMaker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LogoBaseCreatorService>();
        services.AddScoped<LogoIconService>();
        services.AddScoped<LogoFormatService>();
        services.AddScoped<LogoDarkService>();
        services.AddScoped<BrandGuideService>();
        services.AddScoped<LogoOrchestrationService>();
        services.AddScoped<AndroidExportService>();
        services.AddScoped<IosExportService>();
        services.AddScoped<FaviconExportService>();
        services.AddScoped<SocialMediaExportService>();
        return services;
    }
}
