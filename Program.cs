using AiLogoMaker.Services;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("========================================");
Console.WriteLine("         AI LOGO MAKER");
Console.WriteLine("========================================");
Console.WriteLine();

// --- API Key via appsettings.json ---
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var apiKey = configuration["OpenAI:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR-KEY-HERE")
{
    Console.WriteLine("ERROR: Set your API key in appsettings.json");
    return;
}
Console.WriteLine("  API Key loaded from appsettings.json.\n");

// --- Prompts directory ---
var promptsDir = Path.Combine(AppContext.BaseDirectory, "prompts");
if (!Directory.Exists(promptsDir))
    promptsDir = Path.Combine(Directory.GetCurrentDirectory(), "prompts");
if (!Directory.Exists(promptsDir))
{
    Console.WriteLine("ERROR: 'prompts' folder not found.");
    return;
}

// --- Brand Name ---
Console.Write("Brand name: ");
var brandName = Console.ReadLine()?.Trim();
if (string.IsNullOrWhiteSpace(brandName))
{
    Console.WriteLine("ERROR: Brand name is required.");
    return;
}

// --- Description ---
Console.WriteLine("\nDescribe your logo (concept, industry, personality, desired colors):");
Console.Write("> ");
var userPrompt = Console.ReadLine()?.Trim();
if (string.IsNullOrWhiteSpace(userPrompt))
{
    Console.WriteLine("ERROR: Description is required.");
    return;
}

// --- AI picks style and rules ---
var styles = ChatGPTService.GetAvailableStyles(promptsDir);
var rules = ChatGPTService.GetAvailableRules(promptsDir);
var selectedStyle = PickBestStyle(userPrompt, styles);
var selectedRules = PickBestRules(userPrompt, rules);

// --- Output Directory ---
var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output", $"{brandName.ToLower().Replace(" ", "-")}_{timestamp}");
Directory.CreateDirectory(outputDir);

// --- Summary ---
Console.WriteLine("\n========================================");
Console.WriteLine("  GENERATION SUMMARY");
Console.WriteLine("========================================");
Console.WriteLine($"  Brand:  {brandName}");
Console.WriteLine($"  Style:  {selectedStyle}");
Console.WriteLine($"  Rules:  {string.Join(", ", selectedRules)}");
Console.WriteLine($"  Output: {outputDir}");
Console.WriteLine("========================================\n");

Console.Write("Press ENTER to start or CTRL+C to cancel...");
Console.ReadLine();

// --- Generate Logos ---
Console.WriteLine("\n--- PHASE 1: Generating logos with AI ---");
Console.WriteLine("  (This may take a few minutes...)\n");

var chatGptService = new ChatGPTService(apiKey, promptsDir);
var logos = await chatGptService.GenerateLogosAsync(userPrompt, brandName, selectedStyle, selectedRules, outputDir);

if (logos.Count == 0)
{
    Console.WriteLine("\nERROR: No logos were generated.");
    return;
}

Console.WriteLine($"\n  {logos.Count} logo(s) generated successfully!");

// --- Export All Sizes ---
Console.WriteLine("\n--- PHASE 2: Exporting all sizes ---");
Console.WriteLine("  (Android, iOS, Favicon, Social Media)\n");

var exportService = new ImageExportService();
await exportService.ExportAllAsync(logos, outputDir);

// --- Summary ---
ImageExportService.PrintExportSummary(outputDir);

Console.WriteLine("========================================");
Console.WriteLine("  DONE!");
Console.WriteLine("========================================\n");

// --- Helpers: pick style & rules based on description keywords ---

static string PickBestStyle(string description, List<string> available)
{
    var desc = description.ToLowerInvariant();

    // Try to match based on keywords in the user's description
    var keywords = new Dictionary<string, string[]>
    {
        ["wordmark"] = ["typography", "typographic", "text only", "text-only", "wordmark", "lettering", "font", "tipografia", "tipográfico"],
        ["lettermark"] = ["monogram", "initials", "lettermark", "letter", "acronym", "monograma", "iniciais"],
        ["brandmark"] = ["icon", "symbol", "abstract", "brandmark", "minimal icon", "símbolo", "ícone", "abstrato"],
        ["mascote"] = ["mascot", "character", "cartoon", "animal", "creature", "personagem", "mascote"],
        ["emblema"] = ["emblem", "badge", "crest", "seal", "shield", "vintage", "classic", "retro", "brasão", "selo", "emblema"],
        ["combinado"] = ["combined", "icon and text", "symbol and name", "combinado", "combination"],
    };

    foreach (var (style, words) in keywords)
    {
        if (available.Contains(style) && words.Any(w => desc.Contains(w)))
            return style;
    }

    // Default: "combinado" is the most versatile, or random if not available
    if (available.Contains("combinado")) return "combinado";

    return available[Random.Shared.Next(available.Count)];
}

static List<string> PickBestRules(string description, List<string> available)
{
    var desc = description.ToLowerInvariant();
    var picked = new List<string>();

    var keywords = new Dictionary<string, string[]>
    {
        ["regras-minimalismo"] = ["minimal", "simple", "clean", "modern", "sleek", "minimalista"],
        ["regras-flat-design"] = ["flat", "2d", "solid colors", "modern", "digital", "app"],
        ["regras-proporcao-aurea"] = ["golden", "ratio", "proportion", "harmony", "áurea", "fibonacci"],
        ["regras-grid-circular"] = ["circular", "round", "curves", "organic", "circular"],
        ["regras-grid-modular"] = ["grid", "modular", "geometric", "structured", "precision"],
        ["regras-logo-responsivo"] = ["responsive", "scalable", "adaptive", "multi-size", "responsivo"],
    };

    foreach (var (rule, words) in keywords)
    {
        if (available.Contains(rule) && words.Any(w => desc.Contains(w)))
            picked.Add(rule);
    }

    // Always include responsive + at least one aesthetic rule
    if (available.Contains("regras-logo-responsivo") && !picked.Contains("regras-logo-responsivo"))
        picked.Add("regras-logo-responsivo");

    if (picked.Count < 2)
    {
        // Add minimalismo + flat design as sensible defaults
        foreach (var fallback in new[] { "regras-minimalismo", "regras-flat-design" })
        {
            if (available.Contains(fallback) && !picked.Contains(fallback))
                picked.Add(fallback);
        }
    }

    return picked;
}
