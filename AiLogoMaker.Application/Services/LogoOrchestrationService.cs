using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;
using AiLogoMaker.Domain.Services;
using AiLogoMaker.Domain.Services.Export;

namespace AiLogoMaker.Application.Services;

public class LogoOrchestrationService
{
    private readonly LogoBaseCreatorService _baseCreator;
    private readonly LogoIconService _iconService;
    private readonly LogoFormatService _formatService;
    private readonly LogoDarkService _darkService;
    private readonly IImageExportService _imageExport;
    private readonly IPromptRepository _promptRepository;
    private readonly AndroidExportService _androidExport;
    private readonly IosExportService _iosExport;
    private readonly FaviconExportService _faviconExport;
    private readonly SocialMediaExportService _socialMediaExport;
    private readonly BrandGuideService _brandGuide;

    public LogoOrchestrationService(
        LogoBaseCreatorService baseCreator,
        LogoIconService iconService,
        LogoFormatService formatService,
        LogoDarkService darkService,
        IImageExportService imageExport,
        IPromptRepository promptRepository,
        AndroidExportService androidExport,
        IosExportService iosExport,
        FaviconExportService faviconExport,
        SocialMediaExportService socialMediaExport,
        BrandGuideService brandGuide)
    {
        _baseCreator = baseCreator;
        _iconService = iconService;
        _formatService = formatService;
        _darkService = darkService;
        _imageExport = imageExport;
        _promptRepository = promptRepository;
        _androidExport = androidExport;
        _iosExport = iosExport;
        _faviconExport = faviconExport;
        _socialMediaExport = socialMediaExport;
        _brandGuide = brandGuide;
    }

    public List<string> GetAvailableStyles() => _promptRepository.GetAvailableStyles();

    public List<string> GetAvailableRules() => _promptRepository.GetAvailableRules();

    public string PickBestStyle(string description, List<string> available)
    {
        var desc = description.ToLowerInvariant();

        var keywords = new Dictionary<string, string[]>
        {
            ["wordmark"] = ["typography", "typographic", "text only", "text-only", "wordmark", "lettering", "font", "tipografia", "tipográfico"],
            ["lettermark"] = ["monogram", "initials", "lettermark", "letter", "acronym", "monograma", "iniciais"],
            ["brandmark"] = ["icon", "symbol", "abstract", "brandmark", "minimal icon", "símbolo", "ícone", "abstrato"],
            ["mascot"] = ["mascot", "character", "cartoon", "animal", "creature", "personagem", "mascote"],
            ["emblem"] = ["emblem", "badge", "crest", "seal", "shield", "vintage", "classic", "retro", "brasão", "selo", "emblema"],
            ["combined"] = ["combined", "icon and text", "symbol and name", "combinado", "combination"],
        };

        foreach (var (style, words) in keywords)
        {
            if (available.Contains(style) && words.Any(w => desc.Contains(w)))
                return style;
        }

        if (available.Contains("combined")) return "combined";

        return available[Random.Shared.Next(available.Count)];
    }

    public List<string> PickBestRules(string description, List<string> available)
    {
        var desc = description.ToLowerInvariant();
        var picked = new List<string>();

        var keywords = new Dictionary<string, string[]>
        {
            ["rules-minimalism"] = ["minimal", "simple", "clean", "modern", "sleek", "minimalista"],
            ["rules-flat-design"] = ["flat", "2d", "solid colors", "modern", "digital", "app"],
            ["rules-golden-ratio"] = ["golden", "ratio", "proportion", "harmony", "áurea", "fibonacci"],
            ["rules-circular-grid"] = ["circular", "round", "curves", "organic", "circular"],
            ["rules-modular-grid"] = ["grid", "modular", "geometric", "structured", "precision"],
            ["rules-responsive-logo"] = ["responsive", "scalable", "adaptive", "multi-size", "responsivo"],
        };

        foreach (var (rule, words) in keywords)
        {
            if (available.Contains(rule) && words.Any(w => desc.Contains(w)))
                picked.Add(rule);
        }

        if (available.Contains("rules-responsive-logo") && !picked.Contains("rules-responsive-logo"))
            picked.Add("rules-responsive-logo");

        if (picked.Count < 2)
        {
            foreach (var fallback in new[] { "rules-minimalism", "rules-flat-design" })
            {
                if (available.Contains(fallback) && !picked.Contains(fallback))
                    picked.Add(fallback);
            }
        }

        return picked;
    }

    // Step 1: Create the base logo
    public async Task<LogoResult> CreateBaseLogoAsync(
        string userPrompt,
        string brandName,
        string logoStyle,
        List<string> designRules,
        string outputDir,
        string? additionalInstructions = null)
    {
        var effectivePrompt = !string.IsNullOrWhiteSpace(additionalInstructions)
            ? $"{userPrompt}\n\nAdditional instructions: {additionalInstructions}"
            : userPrompt;
        var basePrompt = _baseCreator.BuildBasePrompt(effectivePrompt, brandName, logoStyle, designRules);
        return await _baseCreator.CreateBaseLogoAsync(basePrompt, brandName, outputDir);
    }

    // Step 1.1: Adjust an existing logo based on user feedback
    public async Task<LogoResult> AdjustLogoAsync(
        LogoResult currentLogo, string adjustmentPrompt, string brandName)
    {
        return await _baseCreator.AdjustLogoAsync(currentLogo, adjustmentPrompt, brandName);
    }

    // Step 1.5: Create icon (symbol only, no text)
    public async Task<LogoResult> CreateIconAsync(
        LogoResult baseLogo, string brandName, string outputDir, string? additionalInstructions = null)
    {
        return await _iconService.CreateIconAsync(baseLogo, brandName, outputDir, additionalInstructions);
    }

    // Step 2: Detect format and get missing formats
    public LogoFormat DetectFormat(string imagePath) => _formatService.DetectFormat(imagePath);
    public List<LogoFormat> GetMissingFormats(LogoFormat sourceFormat) => _formatService.GetMissingFormats(sourceFormat);

    // Step 2: Create a single format variant
    public async Task<LogoResult> CreateFormatVariantAsync(
        LogoResult baseLogo, LogoFormat targetFormat, string brandName, string outputDir, string? additionalInstructions = null)
    {
        return await _formatService.CreateFormatVariantAsync(baseLogo, targetFormat, brandName, outputDir, additionalInstructions);
    }

    // Step 3: Create a white variant (no AI)
    public LogoResult CreateWhiteVariant(LogoResult lightLogo, string outputDir)
    {
        return _darkService.CreateWhiteVariant(lightLogo, outputDir);
    }

    // Step 3: Create a single dark variant
    public async Task<LogoResult> CreateDarkVariantAsync(
        LogoResult lightLogo, string brandName, string outputDir, string? additionalInstructions = null)
    {
        return await _darkService.CreateDarkVariantAsync(lightLogo, brandName, outputDir, additionalInstructions);
    }

    // Step 4: Export all sizes
    public async Task ExportAllAsync(List<LogoResult> logos, string outputDir)
    {
        await _androidExport.ExportAsync(logos, outputDir);
        await _iosExport.ExportAsync(logos, outputDir);
        await _faviconExport.ExportAsync(logos, outputDir);
        await _socialMediaExport.ExportAsync(logos, outputDir);
    }

    // Step 5: Generate brand implementation guide
    public async Task GenerateBrandGuideAsync(
        string brandName, string description, List<LogoResult> logos, string outputDir)
    {
        await _brandGuide.GenerateAsync(brandName, description, logos, outputDir);
    }

    // Step 5: Generate only missing prompt files
    public async Task GenerateMissingPromptsAsync(
        string brandName, string description, List<LogoResult> logos, string outputDir)
    {
        await _brandGuide.GenerateMissingPromptsAsync(brandName, description, logos, outputDir);
    }

    public List<string> GetMissingPromptFiles(string outputDir)
    {
        return BrandGuideService.GetMissingPromptFiles(outputDir);
    }

    public ExportSummary GetExportSummary(string outputDir)
    {
        return _imageExport.GetExportSummary(outputDir);
    }
}
