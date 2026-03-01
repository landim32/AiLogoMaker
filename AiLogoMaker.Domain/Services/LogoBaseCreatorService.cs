using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Services;

public class LogoBaseCreatorService
{
    private readonly IAIAppService _aiService;
    private readonly IPromptRepository _promptRepository;

    public LogoBaseCreatorService(IAIAppService aiService, IPromptRepository promptRepository)
    {
        _aiService = aiService;
        _promptRepository = promptRepository;
    }

    public string BuildBasePrompt(string userPrompt, string brandName, string logoStyle, List<string> designRules)
    {
        var parts = new List<string>
        {
            $"Client briefing: {userPrompt}",
            $"Brand name: {brandName}"
        };

        var styleContent = _promptRepository.LoadPromptContent(logoStyle);
        if (styleContent != null)
            parts.Add($"Logo style:\n{styleContent}");

        foreach (var rule in designRules)
        {
            var ruleContent = _promptRepository.LoadPromptContent(rule);
            if (ruleContent != null)
                parts.Add($"Design rule:\n{ruleContent}");
        }

        var colorContent = _promptRepository.LoadPromptContent("color-study");
        if (colorContent != null)
            parts.Add($"Color study:\n{colorContent}");

        return string.Join("\n\n---\n\n", parts);
    }

    public async Task<LogoResult> CreateBaseLogoAsync(string basePrompt, string brandName, string outputDir)
    {
        var originalsDir = Path.Combine(outputDir, "originals");
        Directory.CreateDirectory(originalsDir);

        var prompt = $"""
            Create a professional logo mark for the brand "{brandName}".

            MANDATORY REQUIREMENTS:
            - The background MUST be fully transparent (alpha channel = 0). DO NOT add any white, gray, or solid color background
            - No mockups, no background shadows, no decorative borders
            - Clean, vector-style, professional design
            - Use dark and saturated colors for the logo elements

            {basePrompt}
            """;

        var fileName = "logo-base.png";
        var filePath = Path.Combine(originalsDir, fileName);

        var imageBytes = await _aiService.GenerateImageAsync(prompt, "1024x1024");
        await File.WriteAllBytesAsync(filePath, imageBytes);

        return new LogoResult
        {
            Name = fileName,
            FilePath = filePath,
            Variant = LogoVariant.Square,
            Prompt = prompt
        };
    }

    public async Task<LogoResult> AdjustLogoAsync(LogoResult currentLogo, string adjustmentPrompt, string brandName)
    {
        var info = SixLabors.ImageSharp.Image.Identify(currentLogo.FilePath);
        var size = $"{info.Width}x{info.Height}";

        var prompt = $"""
            Adjust this logo mark for the brand "{brandName}" based on the following feedback:

            {adjustmentPrompt}

            MANDATORY REQUIREMENTS:
            - Keep the overall design concept and identity
            - Apply ONLY the requested adjustments
            - The background MUST be fully transparent (alpha channel = 0). DO NOT add any white, gray, or solid color background
            - No mockups, no background shadows, no decorative borders
            - Clean, vector-style, professional design
            """;

        var imageBytes = await _aiService.EditImageAsync(currentLogo.FilePath, prompt, size);
        await File.WriteAllBytesAsync(currentLogo.FilePath, imageBytes);

        return new LogoResult
        {
            Name = currentLogo.Name,
            FilePath = currentLogo.FilePath,
            Variant = currentLogo.Variant,
            Prompt = prompt
        };
    }
}
