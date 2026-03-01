using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Services.Export;

public class AndroidExportService
{
    private readonly IImageResizeService _resizeService;

    public AndroidExportService(IImageResizeService resizeService)
    {
        _resizeService = resizeService;
    }

    public async Task ExportAsync(List<LogoResult> logos, string outputDir)
    {
        var squareLogo = logos.FirstOrDefault(l => l.Variant == LogoVariant.Square);
        if (squareLogo == null) return;

        var horizontalDark = logos.FirstOrDefault(l => l.Variant == LogoVariant.HorizontalDark);

        await _resizeService.ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetAndroidIcons(outputDir));
        await _resizeService.ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetAndroidAdaptiveIcons(outputDir));

        await _resizeService.ExportSplashAsync(squareLogo.FilePath, ExportPresets.GetAndroidSplash(outputDir, isDark: false), isDark: false);

        if (horizontalDark != null)
            await _resizeService.ExportSplashAsync(squareLogo.FilePath, ExportPresets.GetAndroidSplash(outputDir, isDark: true), isDark: true);
    }
}
