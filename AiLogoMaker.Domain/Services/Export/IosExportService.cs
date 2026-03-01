using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Services.Export;

public class IosExportService
{
    private readonly IImageResizeService _resizeService;

    public IosExportService(IImageResizeService resizeService)
    {
        _resizeService = resizeService;
    }

    public async Task ExportAsync(List<LogoResult> logos, string outputDir)
    {
        var squareLogo = logos.FirstOrDefault(l => l.Variant == LogoVariant.Square);
        if (squareLogo == null) return;

        var verticalDark = logos.FirstOrDefault(l => l.Variant == LogoVariant.VerticalDark);

        await _resizeService.ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetIosIcons(outputDir));
        await _resizeService.GenerateContentsJsonAsync(outputDir);

        await _resizeService.ExportSplashAsync(squareLogo.FilePath, ExportPresets.GetIosSplash(outputDir, isDark: false), isDark: false);

        if (verticalDark != null)
            await _resizeService.ExportSplashAsync(squareLogo.FilePath, ExportPresets.GetIosSplash(outputDir, isDark: true), isDark: true);
    }
}
