using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Interfaces;

public interface IImageResizeService
{
    Task ExportResizedAsync(string sourcePath, List<ExportConfig> configs);
    Task ExportSplashAsync(string logoPath, List<ExportConfig> configs, bool isDark);
    Task ExportSocialMediaAsync(string squareLogoPath, string? horizontalLogoPath, List<ExportConfig> configs);
    Task GenerateContentsJsonAsync(string outputDir);
}
