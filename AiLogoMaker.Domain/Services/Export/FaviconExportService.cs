using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Services.Export;

public class FaviconExportService
{
    private readonly IImageResizeService _resizeService;

    public FaviconExportService(IImageResizeService resizeService)
    {
        _resizeService = resizeService;
    }

    public async Task ExportAsync(List<LogoResult> logos, string outputDir)
    {
        var squareLogo = logos.FirstOrDefault(l => l.Variant == LogoVariant.Square);
        if (squareLogo == null) return;

        await _resizeService.ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetFavicons(outputDir));
    }
}
