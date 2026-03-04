using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AiLogoMaker.Domain.Services;

public class LogoIconService
{
    private readonly IAIAppService _aiService;
    private readonly ILogger<LogoIconService> _logger;

    public LogoIconService(IAIAppService aiService, ILogger<LogoIconService> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<LogoResult> CreateIconAsync(
        LogoResult baseLogo, string brandName, string outputDir, string? additionalInstructions = null)
    {
        var originalsDir = Path.Combine(outputDir, "originals");
        Directory.CreateDirectory(originalsDir);

        var prompt = BuildIconPrompt(brandName);
        if (!string.IsNullOrWhiteSpace(additionalInstructions))
            prompt += $"\n\nADDITIONAL CLIENT INSTRUCTIONS:\n{additionalInstructions}";
        var fileName = "logo-icon.png";
        var filePath = Path.Combine(originalsDir, fileName);

        _logger.LogInformation("[Icon] Prompt used:\n{Prompt}", prompt);

        var imageBytes = await _aiService.EditImageAsync(baseLogo.FilePath, prompt, "1024x1024");
        await File.WriteAllBytesAsync(filePath, imageBytes);

        return new LogoResult
        {
            Name = fileName,
            FilePath = filePath,
            Variant = LogoVariant.Icon,
            Prompt = prompt
        };
    }

    private static string BuildIconPrompt(string brandName)
    {
        return $"""
            Based on this logo for the brand "{brandName}", create a LOGOTYPE (icon/symbol only) version.

            MANDATORY REQUIREMENTS:
            - Extract ONLY the icon/symbol from the original logo, removing ALL text completely
            - No brand name, no tagline, no letters, no words — ONLY the graphic symbol/icon
            - Keep the exact same icon style, colors, and visual identity as the original
            - Square format (1:1 ratio), with the icon centered
            - The background MUST be fully transparent (alpha channel = 0). DO NOT add any white, gray, or solid color background
            - Clean, vector-style, professional design
            - No mockups, no background shadows, no decorative borders
            - The icon should be large enough to fill most of the square canvas with appropriate padding
            """;
    }
}
