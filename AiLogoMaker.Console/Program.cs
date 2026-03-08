using System.Diagnostics;
using AiLogoMaker.Application;
using AiLogoMaker.Application.Services;
using AiLogoMaker.Console;
using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;
using AiLogoMaker.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("========================================");
Console.WriteLine("         AI LOGO MAKER");
Console.WriteLine("========================================");
Console.WriteLine();

// --- Configuration ---
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

// --- DI Container ---
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
services.AddSingleton<IConfiguration>(configuration);
services.AddApplication();
services.AddInfrastructure(configuration);

using var serviceProvider = services.BuildServiceProvider();

var orchestrator = serviceProvider.GetRequiredService<LogoOrchestrationService>();
var sessionRepo = serviceProvider.GetRequiredService<ISessionRepository>();
var sessionManager = new SessionManager(sessionRepo);

var outputRoot = Path.Combine(Directory.GetCurrentDirectory(), "output");

// =============================================
// SESSION: Check for existing sessions
// =============================================
var existingSessions = await sessionManager.FindExistingSessionsAsync(outputRoot);
var isResuming = false;

if (existingSessions.Count > 0)
{
    Console.WriteLine("  Existing sessions found:\n");
    for (var i = 0; i < existingSessions.Count; i++)
    {
        var s = existingSessions[i];
        Console.WriteLine($"    [{i + 1}] {s.BrandName} - {s.CurrentStep} ({s.UpdatedAt:yyyy-MM-dd HH:mm})");
    }
    Console.WriteLine($"    [0] Start a new session\n");
    Console.Write("  Choose: ");
    var choice = Console.ReadLine()?.Trim();

    if (int.TryParse(choice, out var idx) && idx >= 1 && idx <= existingSessions.Count)
    {
        var resumedSession = existingSessions[idx - 1];

        sessionManager.LoadSession(resumedSession);
        isResuming = true;

        Console.WriteLine($"\n  Resuming session: {resumedSession.BrandName}");
        Console.WriteLine($"  Current step: {resumedSession.CurrentStep}");
    }
}

string brandName;
string userPrompt;
string selectedStyle;
List<string> selectedRules;
string outputDir;

if (isResuming)
{
    var session = sessionManager.Current;
    brandName = session.BrandName;
    userPrompt = session.Description;
    selectedStyle = session.SelectedStyle;
    selectedRules = session.SelectedRules;
    outputDir = session.OutputDirectory;

    // Resuming session — always run all steps from the beginning.
    // Each step checks if the file exists on disk; if not, generates it.
    // Every image (existing or newly generated) goes through the approval prompt.
}
else
{
    // --- New Session: Collect inputs ---
    Console.Write("Brand name: ");
    brandName = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(brandName))
    {
        Console.WriteLine("ERROR: Brand name is required.");
        return;
    }

    Console.WriteLine("\nDescribe your logo (concept, industry, personality, desired colors):");
    Console.Write("> ");
    userPrompt = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(userPrompt))
    {
        Console.WriteLine("ERROR: Description is required.");
        return;
    }

    // --- Optional: provide an existing logo ---
    Console.WriteLine("\nDo you have an existing logo to use as the base? (leave empty to generate with AI)");
    Console.Write("Logo file path: ");
    var existingLogoPath = Console.ReadLine()?.Trim().Trim('"');

    var styles = orchestrator.GetAvailableStyles();
    var rules = orchestrator.GetAvailableRules();
    selectedStyle = styles[Random.Shared.Next(styles.Count)];
    selectedRules = orchestrator.PickBestRules(userPrompt, rules);

    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    outputDir = Path.Combine(outputRoot, $"{brandName.ToLower().Replace(" ", "-")}_{timestamp}");
    Directory.CreateDirectory(outputDir);

    await sessionManager.CreateNewSessionAsync(brandName, userPrompt, outputDir, selectedStyle, selectedRules);

    // If user provided an existing logo, copy it and register as approved base
    if (!string.IsNullOrWhiteSpace(existingLogoPath))
    {
        if (!File.Exists(existingLogoPath))
        {
            Console.WriteLine($"ERROR: File not found: {existingLogoPath}");
            return;
        }

        var originalsDir = Path.Combine(outputDir, "originals");
        Directory.CreateDirectory(originalsDir);
        var extension = Path.GetExtension(existingLogoPath);
        var destPath = Path.Combine(originalsDir, $"{brandName.ToLower().Replace(" ", "-")}-base{extension}");
        File.Copy(existingLogoPath, destPath, overwrite: true);

        await sessionManager.RecordImageGeneratedAsync("base", Path.GetFileName(destPath), destPath, LogoVariant.Square, "user-provided");
        await sessionManager.SetImageStatusAsync("base", ImageApprovalStatus.Approved);
        await sessionManager.AdvanceStepAsync(SessionStep.IconLogo);

        Console.WriteLine($"\n  Base logo imported: {destPath}");
    }

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
}

// =============================================
// STEP 1: Create base logo
// =============================================
step1:

Console.WriteLine("\n--- STEP 1: Base Logo ---");

{
    var existingBase = sessionManager.GetImage("base");
    if (existingBase != null && File.Exists(existingBase.FilePath))
    {
        Console.WriteLine($"  Base logo already exists: {existingBase.FilePath}");
        await sessionManager.SetImageStatusAsync("base", ImageApprovalStatus.Approved);
    }
    else
    {
        while (true)
        {
            Console.WriteLine("\n  Additional instructions for base logo (press ENTER to skip):");
            Console.Write("  > ");
            var extraPrompt = Console.ReadLine()?.Trim();

            selectedStyle = orchestrator.GetAvailableStyles()[Random.Shared.Next(orchestrator.GetAvailableStyles().Count)];
            Console.WriteLine($"\n  Creating base logo (style: {selectedStyle})...\n");

            LogoResult baseLogo;
            try
            {
                baseLogo = await orchestrator.CreateBaseLogoAsync(userPrompt, brandName, selectedStyle, selectedRules, outputDir, extraPrompt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n  ERROR: {ex.Message}");
                return;
            }

            await sessionManager.RecordImageGeneratedAsync("base", baseLogo.Name, baseLogo.FilePath, baseLogo.Variant, baseLogo.Prompt);
            Console.WriteLine($"  Base logo created: {baseLogo.FilePath}");
            OpenFile(baseLogo.FilePath);

            var decision = await HandleImageApproval("base", baseLogo, brandName, orchestrator, sessionManager);
            if (decision == "cancelled") return;
            if (decision == "approved") break;
            Console.WriteLine("\n  Generating a new logo...");
        }
    }
}

await sessionManager.AdvanceStepAsync(SessionStep.IconLogo);

// =============================================
// STEP 1.5: Create icon (symbol only, no text)
// =============================================
stepIcon:

Console.WriteLine("\n--- STEP 1.5: Icon (Symbol Only) ---");

{
    var baseImageForIcon = sessionManager.GetImage("base")!;
    var baseLogoForIcon = SessionManager.ToLogoResult(baseImageForIcon);

    var existingIcon = sessionManager.GetImage("icon");
    if (existingIcon != null && File.Exists(existingIcon.FilePath))
    {
        Console.WriteLine($"  Icon already exists: {existingIcon.FilePath}");
        await sessionManager.SetImageStatusAsync("icon", ImageApprovalStatus.Approved);
    }
    else
    {
        while (true)
        {
            Console.WriteLine("\n  Additional instructions for icon (press ENTER to skip):");
            Console.Write("  > ");
            var extraPrompt = Console.ReadLine()?.Trim();

            Console.WriteLine("\n  Creating icon (symbol only, no text)...\n");

            LogoResult iconLogo;
            try
            {
                iconLogo = await orchestrator.CreateIconAsync(baseLogoForIcon, brandName, outputDir, extraPrompt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n  ERROR: {ex.Message}");
                return;
            }

            await sessionManager.RecordImageGeneratedAsync("icon", iconLogo.Name, iconLogo.FilePath, iconLogo.Variant, iconLogo.Prompt);
            Console.WriteLine($"  Icon created: {iconLogo.FilePath}");
            OpenFile(iconLogo.FilePath);

            var decision = await HandleImageApproval("icon", iconLogo, brandName, orchestrator, sessionManager);
            if (decision == "cancelled") return;
            if (decision == "approved") break;
            Console.WriteLine("\n  Generating a new icon...");
        }
    }
}

await sessionManager.AdvanceStepAsync(SessionStep.FormatVariants);

// =============================================
// STEP 2: Create format variants (individual approval)
// =============================================
step2:

Console.WriteLine("\n--- STEP 2: Format Variants ---");

var baseImage = sessionManager.GetImage("base")!;
var baseLogoForVariants = SessionManager.ToLogoResult(baseImage);
var sourceFormat = orchestrator.DetectFormat(baseLogoForVariants.FilePath);
var missingFormats2 = orchestrator.GetMissingFormats(sourceFormat);

foreach (var format in missingFormats2)
{
    var imageId = format switch
    {
        LogoFormat.Horizontal => "horizontal-light",
        LogoFormat.Vertical => "vertical-light",
        LogoFormat.Square => "square-light",
        _ => throw new ArgumentOutOfRangeException()
    };

    var existingImg = sessionManager.GetImage(imageId);
    if (existingImg != null && File.Exists(existingImg.FilePath))
    {
        Console.WriteLine($"  {imageId} already exists: {existingImg.FilePath}");
        await sessionManager.SetImageStatusAsync(imageId, ImageApprovalStatus.Approved);
        continue;
    }

    while (true)
    {
        Console.WriteLine($"\n  Additional instructions for {format} variant (press ENTER to skip):");
        Console.Write("  > ");
        var extraPrompt = Console.ReadLine()?.Trim();

        Console.WriteLine($"\n  Creating {format} variant...\n");

        LogoResult variant;
        try
        {
            variant = await orchestrator.CreateFormatVariantAsync(baseLogoForVariants, format, brandName, outputDir, extraPrompt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n  ERROR: {ex.Message}");
            return;
        }

        await sessionManager.RecordImageGeneratedAsync(imageId, variant.Name, variant.FilePath, variant.Variant, variant.Prompt);
        Console.WriteLine($"  Variant created: {variant.FilePath}");
        OpenFile(variant.FilePath);

        var decision = await HandleImageApproval(imageId, variant, brandName, orchestrator, sessionManager);
        if (decision == "cancelled") return;
        if (decision == "approved") break;
        Console.WriteLine($"\n  Regenerating {format} variant...");
    }
}

await sessionManager.AdvanceStepAsync(SessionStep.DarkVariants);

// =============================================
// STEP 3: Dark variants (individual approval)
// =============================================
step3:

Console.WriteLine("\n--- STEP 3: Dark Variants ---");

// Collect all light images that have files on disk (regardless of status)
var lightImageIds = new[] { "base", "icon", "horizontal-light", "vertical-light", "square-light" };
var lightImages = lightImageIds
    .Select(id => sessionManager.GetImage(id))
    .Where(img => img != null && File.Exists(img.FilePath))
    .ToList();

foreach (var lightImg in lightImages)
{
    var darkImageId = lightImg!.ImageId switch
    {
        "base" => "base-dark",
        "icon" => "icon-dark",
        _ => lightImg.ImageId.Replace("-light", "-dark")
    };

    var existingDark = sessionManager.GetImage(darkImageId);
    if (existingDark != null && File.Exists(existingDark.FilePath))
    {
        Console.WriteLine($"  {darkImageId} already exists: {existingDark.FilePath}");
        await sessionManager.SetImageStatusAsync(darkImageId, ImageApprovalStatus.Approved);
        continue;
    }

    var lightLogo = SessionManager.ToLogoResult(lightImg);

    // First attempt: white variant (no AI)
    Console.WriteLine($"\n  Creating white variant for {lightImg.ImageId} (no AI)...");
    var whiteVariant = orchestrator.CreateWhiteVariant(lightLogo, outputDir);
    await sessionManager.RecordImageGeneratedAsync(darkImageId, whiteVariant.Name, whiteVariant.FilePath, whiteVariant.Variant, whiteVariant.Prompt);
    Console.WriteLine($"  White variant created: {whiteVariant.FilePath}");
    OpenFile(whiteVariant.FilePath);

    var whiteDecision = await HandleImageApproval(darkImageId, whiteVariant, brandName, orchestrator, sessionManager);
    if (whiteDecision == "cancelled") return;
    if (whiteDecision == "approved") continue;

    // Rejected: fall through to AI generation
    while (true)
    {
        Console.WriteLine($"\n  Additional instructions for {darkImageId} (press ENTER to skip):");
        Console.Write("  > ");
        var extraPrompt = Console.ReadLine()?.Trim();

        Console.WriteLine($"\n  Creating dark variant for {lightImg.ImageId} (AI)...\n");

        LogoResult darkVariant;
        try
        {
            darkVariant = await orchestrator.CreateDarkVariantAsync(lightLogo, brandName, outputDir, extraPrompt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n  ERROR: {ex.Message}");
            return;
        }

        await sessionManager.RecordImageGeneratedAsync(darkImageId, darkVariant.Name, darkVariant.FilePath, darkVariant.Variant, darkVariant.Prompt);
        Console.WriteLine($"  Dark variant created: {darkVariant.FilePath}");
        OpenFile(darkVariant.FilePath);

        var decision = await HandleImageApproval(darkImageId, darkVariant, brandName, orchestrator, sessionManager);
        if (decision == "cancelled") return;
        if (decision == "approved") break;
        Console.WriteLine($"\n  Regenerating dark variant for {lightImg.ImageId}...");
    }
}

await sessionManager.AdvanceStepAsync(SessionStep.Export);

// =============================================
// STEP 4: Export all sizes
// =============================================
step4:

Console.WriteLine("\n--- STEP 4: Exporting all sizes ---");
Console.WriteLine("  (Android, iOS, Favicon, Social Media)\n");

var allApproved = sessionManager.Current.Images
    .Where(i => i.Status == ImageApprovalStatus.Approved)
    .ToList();
var allLogos = SessionManager.ToLogoResults(allApproved);

await orchestrator.ExportAllAsync(allLogos, outputDir);

// --- Brand Implementation Guide ---
Console.WriteLine("\n  Generating brand implementation guide...");
await orchestrator.GenerateBrandGuideAsync(brandName, userPrompt, allLogos, outputDir);
Console.WriteLine($"  Brand guide saved: {Path.Combine(outputDir, "brand-implementation-guide.md")}");

await sessionManager.AdvanceStepAsync(SessionStep.Completed);

// --- Summary ---
var summary = orchestrator.GetExportSummary(outputDir);

Console.WriteLine("\n========================================");
Console.WriteLine("  EXPORT SUMMARY");
Console.WriteLine("========================================\n");

foreach (var category in summary.Categories)
{
    if (category.Generated)
        Console.WriteLine($"  {category.Name}: {category.FileCount} file(s)");
    else
        Console.WriteLine($"  {category.Name}: (not generated)");
}

Console.WriteLine($"\n  TOTAL: {summary.TotalFiles} file(s)");
Console.WriteLine($"  Output: {summary.OutputPath}");
Console.WriteLine();

Console.WriteLine("========================================");
Console.WriteLine("  DONE!");
Console.WriteLine("========================================\n");

return;

// =============================================
// Helper: Individual image approval loop
// =============================================
static async Task<string> HandleImageApproval(
    string imageId,
    LogoResult currentLogo,
    string brandName,
    LogoOrchestrationService orchestrator,
    SessionManager sessionManager)
{
    while (true)
    {
        Console.WriteLine($"\n  Review: {currentLogo.Name}");
        Console.WriteLine("  [1] Approve   - Continue to next step");
        Console.WriteLine("  [2] Adjust    - Request changes to this image");
        Console.WriteLine("  [3] Reject    - Discard and generate a new one");
        Console.WriteLine("  [4] Cancel    - Abort the entire process");
        Console.Write("\n  Choose (1-4): ");

        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        var action = input switch
        {
            "1" or "approve" or "aprovar" => "approved",
            "2" or "adjust" or "ajustar" => "adjust",
            "3" or "reject" or "reprovar" => "rejected",
            "4" or "cancel" or "cancelar" => "cancelled",
            _ => "adjust"
        };

        if (action == "approved")
        {
            await sessionManager.SetImageStatusAsync(imageId, ImageApprovalStatus.Approved);
            Console.WriteLine("  Approved!");
            return "approved";
        }

        if (action == "cancelled")
        {
            Console.WriteLine("\n  Generation cancelled by user.");
            return "cancelled";
        }

        if (action == "rejected")
        {
            await sessionManager.SetImageStatusAsync(imageId, ImageApprovalStatus.Rejected);
            return "rejected";
        }

        // action == "adjust"
        Console.WriteLine("\n  Describe the adjustments you want:");
        Console.Write("  > ");
        var adjustmentPrompt = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(adjustmentPrompt))
        {
            Console.WriteLine("  No adjustment provided, try again.");
            continue;
        }

        Console.WriteLine("\n  Adjusting image...\n");
        try
        {
            var adjusted = await orchestrator.AdjustLogoAsync(currentLogo, adjustmentPrompt, brandName);
            await sessionManager.RecordAdjustmentAsync(imageId, adjustmentPrompt, adjusted.Prompt, adjusted.FilePath);
            currentLogo = adjusted;
            Console.WriteLine($"  Adjusted image saved: {adjusted.FilePath}");
            OpenFile(adjusted.FilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n  ERROR: {ex.Message}");
            Console.WriteLine("  Adjustment failed, try again.");
        }
    }
}

static void OpenFile(string filePath)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    catch
    {
        Console.WriteLine($"  (Could not open file automatically. Open manually: {filePath})");
    }
}
