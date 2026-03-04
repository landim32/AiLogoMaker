using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AiLogoMaker.Domain.Services;

public class LogoBaseCreatorService
{
    private readonly IAIAppService _aiService;
    private readonly IPromptRepository _promptRepository;
    private readonly ILogger<LogoBaseCreatorService> _logger;

    public LogoBaseCreatorService(IAIAppService aiService, IPromptRepository promptRepository, ILogger<LogoBaseCreatorService> logger)
    {
        _aiService = aiService;
        _promptRepository = promptRepository;
        _logger = logger;
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

        // Color psychology analysis based on the client description
        var colorDirective = AnalyzeColorPsychology(userPrompt);
        parts.Add($"Color directive:\n{colorDirective}");

        var colorContent = _promptRepository.LoadPromptContent("color-study");
        if (colorContent != null)
            parts.Add($"Color study reference:\n{colorContent}");

        return string.Join("\n\n---\n\n", parts);
    }

    private static string AnalyzeColorPsychology(string description)
    {
        var desc = description.ToLowerInvariant();

        var suggestions = new List<string>();

        // Food & Restaurant
        if (ContainsAny(desc, "food", "restaurant", "pizza", "burger", "chef", "cook", "kitchen", "bakery",
            "café", "cafe", "coffee", "comida", "restaurante", "padaria", "cozinha", "culinária"))
            suggestions.Add("RED or ORANGE (appetite stimulation, energy, warmth) or YELLOW (joy, attention) or BROWN/WARM TONES (artisanal, comfort)");

        // Health & Wellness
        if (ContainsAny(desc, "health", "wellness", "medical", "hospital", "clinic", "pharma", "therapy",
            "saúde", "médico", "clínica", "terapia", "bem-estar", "farmácia"))
            suggestions.Add("TEAL or SOFT GREEN (healing, calm) or LIGHT BLUE (trust, cleanliness) — avoid aggressive reds");

        // Nature & Environment
        if (ContainsAny(desc, "nature", "eco", "organic", "sustainable", "garden", "plant", "green", "earth",
            "natureza", "orgânico", "sustentável", "jardim", "planta", "ambiental"))
            suggestions.Add("GREEN (nature, growth, sustainability) or EARTH TONES (brown, olive, terracotta)");

        // Technology & Software
        if (ContainsAny(desc, "tech", "software", "app", "digital", "ai", "data", "cloud", "cyber", "saas",
            "tecnologia", "aplicativo", "inteligência artificial", "dados"))
            suggestions.Add("Consider NON-DEFAULT colors: ORANGE (innovation, like Firefox/SoundCloud), BLACK+ACCENT (premium tech, like Apple), CORAL/SALMON (modern, like Asana) or TEAL (fresh, like Canva) — AVOID generic blue/purple unless strongly justified");

        // Finance & Business
        if (ContainsAny(desc, "finance", "bank", "invest", "insurance", "accounting", "consult",
            "finanças", "banco", "investimento", "seguro", "contabilidade", "consultoria"))
            suggestions.Add("DARK BLUE or NAVY (trust, stability) or GREEN (prosperity, growth) or GOLD/DARK GOLD (premium, wealth)");

        // Children & Education
        if (ContainsAny(desc, "kids", "children", "school", "education", "learn", "toy", "play",
            "criança", "infantil", "escola", "educação", "aprender", "brinquedo"))
            suggestions.Add("YELLOW (joy, optimism) or ORANGE (playful energy) or MULTICOLOR palette (fun, diversity) — bright and saturated tones");

        // Fashion & Beauty
        if (ContainsAny(desc, "fashion", "beauty", "cosmetic", "style", "luxury", "elegant", "glamour",
            "moda", "beleza", "cosmético", "estilo", "luxo", "elegante"))
            suggestions.Add("BLACK (sophistication, luxury) or GOLD (premium) or PINK/ROSE (modern femininity) or DEEP PURPLE (royalty, creativity)");

        // Sports & Fitness
        if (ContainsAny(desc, "sport", "fitness", "gym", "training", "athletic", "run", "workout",
            "esporte", "academia", "treino", "atlético", "corrida"))
            suggestions.Add("RED or ORANGE (energy, action, adrenaline) or BLACK+NEON accent (power, intensity) or ELECTRIC BLUE (performance)");

        // Creative & Art
        if (ContainsAny(desc, "creative", "art", "design", "studio", "photo", "music", "film",
            "criativo", "arte", "estúdio", "foto", "música", "filme"))
            suggestions.Add("CORAL/SALMON (creative warmth) or YELLOW (inspiration) or MULTICOLOR (creativity, diversity) or BLACK+VIBRANT accent");

        // Legal & Professional Services
        if (ContainsAny(desc, "law", "legal", "attorney", "lawyer", "advocate",
            "advocacia", "advogado", "jurídico", "direito"))
            suggestions.Add("NAVY BLUE (authority, trust) or DARK RED/BURGUNDY (tradition, power) or GOLD accent (prestige)");

        // Construction & Real Estate
        if (ContainsAny(desc, "construction", "building", "real estate", "architecture", "property",
            "construção", "imóvel", "imobiliária", "arquitetura", "engenharia"))
            suggestions.Add("ORANGE (energy, building) or DARK BLUE (reliability) or EARTH TONES (stability, foundation) or YELLOW (safety, construction)");

        // Pet & Animal
        if (ContainsAny(desc, "pet", "animal", "dog", "cat", "vet", "veterinar",
            "pet", "animal", "cachorro", "gato", "veterinár"))
            suggestions.Add("WARM ORANGE (friendly, playful) or GREEN (nature, health) or BROWN (earthy, natural) — avoid cold colors");

        if (suggestions.Count == 0)
        {
            suggestions.Add("Analyze the brand description carefully and choose colors based on color psychology. DO NOT default to blue, green, or purple");
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("IMPORTANT — COLOR PSYCHOLOGY ANALYSIS:");
        sb.AppendLine();
        sb.AppendLine("Based on the brand description, the following color recommendations apply:");
        foreach (var s in suggestions)
            sb.AppendLine($"  - {s}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL COLOR RULES:");
        sb.AppendLine("  - DO NOT default to blue, green, or purple unless the brand's industry specifically calls for it");
        sb.AppendLine("  - Choose colors that match the EMOTION and INDUSTRY of the brand");
        sb.AppendLine("  - Use the color psychology recommendations above as your primary guide");
        sb.AppendLine("  - The chosen colors must create an emotional connection with the target audience");
        sb.AppendLine("  - If the client's description mentions specific colors, ALWAYS prioritize those");
        sb.AppendLine("  - Use a professional color harmony (complementary, analogous, or split-complementary)");
        sb.AppendLine("  - Limit to 2-3 main colors maximum");

        return sb.ToString();
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(k => text.Contains(k));
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

        _logger.LogInformation("[Base Logo] Prompt used:\n{Prompt}", prompt);

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

        _logger.LogInformation("[Adjust Logo] Prompt used:\n{Prompt}", prompt);

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
