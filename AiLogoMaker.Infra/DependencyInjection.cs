using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Infra.AppServices;
using AiLogoMaker.Infra.Repositories;
using AiLogoMaker.Infra.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiLogoMaker.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey not found in configuration.");

        var configuredPath = configuration["Prompts:Path"];
        string promptsDir;

        if (!string.IsNullOrWhiteSpace(configuredPath) && Path.IsPathRooted(configuredPath))
        {
            promptsDir = configuredPath;
        }
        else
        {
            var relativePath = configuredPath ?? "prompts";
            var solutionRoot = FindSolutionRoot();
            promptsDir = Path.Combine(solutionRoot, relativePath);
        }

        services.AddSingleton<IPromptRepository>(new FileSystemPromptRepository(promptsDir));

        services.AddScoped<IAIAppService>(sp =>
            new ChatGPTAppService(
                apiKey,
                sp.GetRequiredService<ILogger<ChatGPTAppService>>()));

        services.AddSingleton<ISessionRepository, FileSystemSessionRepository>();

        services.AddScoped<ImageSharpExportService>();
        services.AddScoped<IImageExportService>(sp => sp.GetRequiredService<ImageSharpExportService>());
        services.AddScoped<IImageResizeService>(sp => sp.GetRequiredService<ImageSharpExportService>());

        return services;
    }

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}
