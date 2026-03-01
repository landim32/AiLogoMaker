using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Services;

public class LogoDarkService
{
    private readonly IAIAppService _aiService;

    public LogoDarkService(IAIAppService aiService)
    {
        _aiService = aiService;
    }

    public async Task<LogoResult> CreateDarkVariantAsync(
        LogoResult sourceLogo, string brandName, string outputDir)
    {
        var originalsDir = Path.Combine(outputDir, "originals");
        Directory.CreateDirectory(originalsDir);

        var darkFileName = sourceLogo.Variant switch
        {
            LogoVariant.Icon => sourceLogo.Name.Replace("-icon.", "-icon-dark."),
            LogoVariant.Square => sourceLogo.Name.Replace("-base.", "-base-dark."),
            _ => sourceLogo.Name.Replace("-light.", "-dark.")
        };
        var darkVariant = sourceLogo.Variant switch
        {
            LogoVariant.HorizontalLight => LogoVariant.HorizontalDark,
            LogoVariant.VerticalLight => LogoVariant.VerticalDark,
            LogoVariant.Icon => LogoVariant.IconDark,
            _ => LogoVariant.VerticalDark
        };

        var info = SixLabors.ImageSharp.Image.Identify(sourceLogo.FilePath);
        var size = $"{info.Width}x{info.Height}";

        var prompt = sourceLogo.Variant == LogoVariant.Icon
            ? BuildIconDarkPrompt(brandName)
            : BuildDarkPrompt(brandName);
        var filePath = Path.Combine(originalsDir, darkFileName);

        var imageBytes = await _aiService.EditImageAsync(sourceLogo.FilePath, prompt, size);
        await File.WriteAllBytesAsync(filePath, imageBytes);

        return new LogoResult
        {
            Name = darkFileName,
            FilePath = filePath,
            Variant = darkVariant,
            Prompt = prompt
        };
    }

    private static string BuildDarkPrompt(string brandName)
    {
        return $"""
            Recreate this exact same logo mark for the brand "{brandName}" as a DARK version, intended for use on dark/black backgrounds.

            MANDATORY REQUIREMENTS:
            - Keep the exact same design, layout, shapes, and proportions as the original logo
            - The background MUST be fully transparent (alpha channel = 0). DO NOT add any white, gray, black, or solid color background
            - Adapt colors for dark backgrounds: use light and vibrant colors for text and icon
            - Text must be white or a light color that contrasts well with dark/black backgrounds
            - Keep the icon recognizable and consistent with the original
            - No mockups, no background shadows, no decorative borders
            """;
    }

    private static string BuildIconDarkPrompt(string brandName)
    {
        return $"""
            Recreate this exact same icon/symbol for the brand "{brandName}" as a DARK version, intended for use on dark/black backgrounds.

            MANDATORY REQUIREMENTS:
            - This is an ICON ONLY — DO NOT add any text, brand name, letters, or words. ONLY the graphic symbol/icon
            - Keep the exact same icon design, shapes, and proportions as the original
            - The background MUST be fully transparent (alpha channel = 0). DO NOT add any white, gray, black, or solid color background
            - Adapt colors for dark backgrounds: use light and vibrant colors for the icon
            - Keep the icon recognizable and consistent with the original
            - No mockups, no background shadows, no decorative borders
            - No text whatsoever
            """;
    }
}
