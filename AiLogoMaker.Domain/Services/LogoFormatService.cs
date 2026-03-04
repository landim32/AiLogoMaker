using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AiLogoMaker.Domain.Services;

public class LogoFormatService
{
    private readonly IAIAppService _aiService;
    private readonly ILogger<LogoFormatService> _logger;

    public LogoFormatService(IAIAppService aiService, ILogger<LogoFormatService> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public LogoFormat DetectFormat(string imagePath)
    {
        var info = SixLabors.ImageSharp.Image.Identify(imagePath);

        var width = info.Width;
        var height = info.Height;

        var ratio = (double)width / height;

        if (ratio > 1.15)
            return LogoFormat.Horizontal;
        if (ratio < 0.85)
            return LogoFormat.Vertical;

        return LogoFormat.Square;
    }

    public async Task<LogoResult> CreateFormatVariantAsync(
        LogoResult sourceLogo, LogoFormat targetFormat, string brandName, string outputDir, string? additionalInstructions = null)
    {
        var originalsDir = Path.Combine(outputDir, "originals");
        Directory.CreateDirectory(originalsDir);

        var (prompt, size, fileName, variant) = targetFormat switch
        {
            LogoFormat.Horizontal => (
                BuildHorizontalPrompt(brandName),
                "1536x1024",
                "logo-horizontal-light.png",
                LogoVariant.HorizontalLight),
            LogoFormat.Vertical => (
                BuildVerticalPrompt(brandName),
                "1024x1536",
                "logo-vertical-light.png",
                LogoVariant.VerticalLight),
            LogoFormat.Square => (
                BuildSquarePrompt(brandName),
                "1024x1024",
                "logo-square-light.png",
                LogoVariant.Square),
            _ => throw new ArgumentOutOfRangeException(nameof(targetFormat))
        };

        if (!string.IsNullOrWhiteSpace(additionalInstructions))
            prompt += $"\n\nADDITIONAL CLIENT INSTRUCTIONS:\n{additionalInstructions}";

        _logger.LogInformation("[Format Variant - {Format}] Prompt used:\n{Prompt}", targetFormat, prompt);

        var filePath = Path.Combine(originalsDir, fileName);
        var imageBytes = await _aiService.EditImageAsync(sourceLogo.FilePath, prompt, size);
        await File.WriteAllBytesAsync(filePath, imageBytes);

        return new LogoResult
        {
            Name = fileName,
            FilePath = filePath,
            Variant = variant,
            Prompt = prompt
        };
    }

    public List<LogoFormat> GetMissingFormats(LogoFormat sourceFormat)
    {
        var allFormats = new[] { LogoFormat.Square, LogoFormat.Horizontal, LogoFormat.Vertical };
        return allFormats.Where(f => f != sourceFormat).ToList();
    }

    private static string BuildHorizontalPrompt(string brandName)
    {
        return $"""
            Recreate this exact same logo mark for the brand "{brandName}" in a HORIZONTAL layout (3:2 landscape ratio).

            MANDATORY REQUIREMENTS:
            - Keep the same visual identity, colors, and style as the original logo
            - Horizontal/landscape format, 3:2 ratio
            - Icon/symbol on the LEFT and brand name "{brandName}" on the RIGHT
            - Horizontal layout with aligned elements
            - The background MUST be fully transparent (alpha channel = 0). DO NOT add any white, gray, or solid color background
            - The name must be clearly legible
            - Elegant and modern typography
            - No mockups, no background shadows, no decorative borders
            - Use dark and saturated colors for the logo elements
            """;
    }

    private static string BuildVerticalPrompt(string brandName)
    {
        return $"""
            Recreate this exact same logo mark for the brand "{brandName}" in a VERTICAL layout (2:3 portrait ratio).

            MANDATORY REQUIREMENTS:
            - Keep the same visual identity, colors, and style as the original logo
            - Vertical/portrait format, 2:3 ratio
            - Icon/symbol on TOP and brand name "{brandName}" BELOW
            - Stacked vertical layout
            - The background MUST be fully transparent (alpha channel = 0). DO NOT add any white, gray, or solid color background
            - The name must be clearly legible
            - Elegant and modern typography
            - No mockups, no background shadows, no decorative borders
            - Use dark and saturated colors for the logo elements
            """;
    }

    private static string BuildSquarePrompt(string brandName)
    {
        return $"""
            Recreate this exact same logo mark for the brand "{brandName}" in a SQUARE layout (1:1 ratio).

            MANDATORY REQUIREMENTS:
            - Keep the same visual identity, colors, and style as the original logo
            - Square format, 1:1 ratio
            - The background MUST be fully transparent (alpha channel = 0). DO NOT add any white, gray, or solid color background
            - Clean, vector-style, professional design
            - No mockups, no background shadows, no decorative borders
            - Use dark and saturated colors for the logo elements
            """;
    }
}
