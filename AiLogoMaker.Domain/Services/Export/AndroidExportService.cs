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

        var verticalLight = logos.FirstOrDefault(l => l.Variant == LogoVariant.VerticalLight);
        var verticalDark = logos.FirstOrDefault(l => l.Variant == LogoVariant.VerticalDark);

        await _resizeService.ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetAndroidIcons(outputDir));
        await _resizeService.ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetAndroidAdaptiveIcons(outputDir));

        var splashLightSource = verticalLight ?? squareLogo;
        await _resizeService.ExportSplashAsync(splashLightSource.FilePath, ExportPresets.GetAndroidSplash(outputDir, isDark: false), isDark: false);

        if (verticalDark != null)
            await _resizeService.ExportSplashAsync(verticalDark.FilePath, ExportPresets.GetAndroidSplash(outputDir, isDark: true), isDark: true);
    }
}
