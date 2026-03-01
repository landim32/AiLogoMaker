using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Services.Export;

public class SocialMediaExportService
{
    private readonly IImageResizeService _resizeService;

    public SocialMediaExportService(IImageResizeService resizeService)
    {
        _resizeService = resizeService;
    }

    public async Task ExportAsync(List<LogoResult> logos, string outputDir)
    {
        var squareLogo = logos.FirstOrDefault(l => l.Variant == LogoVariant.Square);
        if (squareLogo == null) return;

        var horizontalLight = logos.FirstOrDefault(l => l.Variant == LogoVariant.HorizontalLight);

        await _resizeService.ExportSocialMediaAsync(
            squareLogo.FilePath,
            horizontalLight?.FilePath,
            ExportPresets.GetSocialMedia(outputDir));
    }
}
