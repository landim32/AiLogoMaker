using AiLogoMaker.Domain.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AiLogoMaker.Domain.Services;

public class BrandGuideService
{
    public static readonly string[] PromptFileNames =
    {
        "prompt-react.md",
        "prompt-desktop.md",
        "prompt-mobile.md"
    };

    public async Task GenerateAsync(
        string brandName, string description, List<LogoResult> approvedLogos, string outputDir)
    {
        var allColors = new List<(int r, int g, int b)>();

        foreach (var logo in approvedLogos)
        {
            if (!File.Exists(logo.FilePath)) continue;
            var colors = ExtractDominantColors(logo.FilePath);
            allColors.AddRange(colors);
        }

        var palette = ConsolidatePalette(allColors, maxColors: 8);
        var context = BuildContext(brandName, description, approvedLogos, palette);

        var promptsDir = Path.Combine(outputDir, "prompts");
        Directory.CreateDirectory(promptsDir);

        // Generate each prompt individually
        var prompts = BuildPrompts(context);
        for (var i = 0; i < prompts.Count; i++)
        {
            var promptPath = Path.Combine(promptsDir, PromptFileNames[i]);
            await File.WriteAllTextAsync(promptPath, prompts[i]);
        }

        // Generate the main guide markdown
        var markdown = BuildMarkdown(context, prompts);
        var guidePath = Path.Combine(outputDir, "brand-implementation-guide.md");
        await File.WriteAllTextAsync(guidePath, markdown);
    }

    public async Task GenerateMissingPromptsAsync(
        string brandName, string description, List<LogoResult> approvedLogos, string outputDir)
    {
        var promptsDir = Path.Combine(outputDir, "prompts");
        var guidePath = Path.Combine(outputDir, "brand-implementation-guide.md");

        // Check which files are missing
        var missingPrompts = PromptFileNames
            .Where(f => !File.Exists(Path.Combine(promptsDir, f)))
            .ToList();
        var guideMissing = !File.Exists(guidePath);

        if (missingPrompts.Count == 0 && !guideMissing)
            return;

        // Extract colors and build context
        var allColors = new List<(int r, int g, int b)>();
        foreach (var logo in approvedLogos)
        {
            if (!File.Exists(logo.FilePath)) continue;
            allColors.AddRange(ExtractDominantColors(logo.FilePath));
        }

        var palette = ConsolidatePalette(allColors, maxColors: 8);
        var context = BuildContext(brandName, description, approvedLogos, palette);
        var prompts = BuildPrompts(context);

        Directory.CreateDirectory(promptsDir);

        // Write only the missing prompt files
        for (var i = 0; i < PromptFileNames.Length; i++)
        {
            var path = Path.Combine(promptsDir, PromptFileNames[i]);
            if (!File.Exists(path))
                await File.WriteAllTextAsync(path, prompts[i]);
        }

        // Write the guide if missing
        if (guideMissing)
            await File.WriteAllTextAsync(guidePath, BuildMarkdown(context, prompts));
    }

    public static List<string> GetMissingPromptFiles(string outputDir)
    {
        var promptsDir = Path.Combine(outputDir, "prompts");
        var missing = new List<string>();

        foreach (var fileName in PromptFileNames)
        {
            if (!File.Exists(Path.Combine(promptsDir, fileName)))
                missing.Add(fileName);
        }

        if (!File.Exists(Path.Combine(outputDir, "brand-implementation-guide.md")))
            missing.Add("brand-implementation-guide.md");

        return missing;
    }

    private static List<(int r, int g, int b)> ExtractDominantColors(string imagePath)
    {
        using var image = Image.Load<Rgba32>(imagePath);

        // Resize for performance
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(64, 64),
            Mode = ResizeMode.Max
        }));

        var colorCounts = new Dictionary<(int r, int g, int b), int>();

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];

                    // Skip transparent or near-transparent pixels
                    if (pixel.A < 128) continue;

                    // Skip near-white and near-black pixels
                    if (pixel.R > 240 && pixel.G > 240 && pixel.B > 240) continue;
                    if (pixel.R < 15 && pixel.G < 15 && pixel.B < 15) continue;

                    // Quantize to reduce palette (round to nearest 16)
                    var key = (
                        r: (pixel.R / 16) * 16,
                        g: (pixel.G / 16) * 16,
                        b: (pixel.B / 16) * 16
                    );

                    if (colorCounts.ContainsKey(key))
                        colorCounts[key]++;
                    else
                        colorCounts[key] = 1;
                }
            }
        });

        return colorCounts
            .OrderByDescending(c => c.Value)
            .Take(10)
            .Select(c => c.Key)
            .ToList();
    }

    private static List<string> ConsolidatePalette(
        List<(int r, int g, int b)> allColors, int maxColors)
    {
        if (allColors.Count == 0)
            return new List<string> { "#333333", "#666666", "#FFFFFF" };

        // Group similar colors (within distance 40)
        var groups = new List<((int r, int g, int b) color, int count)>();

        foreach (var color in allColors)
        {
            var found = false;
            for (var i = 0; i < groups.Count; i++)
            {
                if (ColorDistance(color, groups[i].color) < 40)
                {
                    groups[i] = (groups[i].color, groups[i].count + 1);
                    found = true;
                    break;
                }
            }
            if (!found)
                groups.Add((color, 1));
        }

        return groups
            .OrderByDescending(g => g.count)
            .Take(maxColors)
            .Select(g => $"#{g.color.r:X2}{g.color.g:X2}{g.color.b:X2}")
            .ToList();
    }

    private static double ColorDistance((int r, int g, int b) a, (int r, int g, int b) b)
    {
        var dr = a.r - b.r;
        var dg = a.g - b.g;
        var db = a.b - b.b;
        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    private record PromptContext(
        string BrandName, string Description, List<LogoResult> Logos, List<string> Palette,
        string PrimaryColor, string SecondaryColor, string AccentColor,
        string ExtraColors, string ExtraCssVars, string LogoFileNames,
        string IconRef, string IconDarkRef,
        string HeaderLogoLight, string HeaderLogoDesktop, string HeaderLogoMobile,
        string PaletteTable, string LogoTable);

    private static PromptContext BuildContext(
        string brandName, string description,
        List<LogoResult> logos, List<string> palette)
    {
        var primaryColor = palette.Count > 0 ? palette[0] : "#333333";
        var secondaryColor = palette.Count > 1 ? palette[1] : "#666666";
        var accentColor = palette.Count > 2 ? palette[2] : "#999999";

        var hasIcon = logos.Any(l => l.Variant == LogoVariant.Icon);
        var hasHorizontal = logos.Any(l => l.Variant is LogoVariant.HorizontalLight or LogoVariant.HorizontalDark);

        var paletteTable = string.Join("\n", palette.Select((c, i) =>
        {
            var role = i switch { 0 => "Primary", 1 => "Secondary", 2 => "Accent", _ => $"Palette {i + 1}" };
            return $"| {role} | `{c}` | ![{c}](https://via.placeholder.com/20/{c.TrimStart('#')}/{c.TrimStart('#')}) |";
        }));

        var logoTable = string.Join("\n", logos.Select(l =>
        {
            var usage = l.Variant switch
            {
                LogoVariant.Square => "App icons, favicons, profile avatars, square placements",
                LogoVariant.Icon => "App icons (no text), favicons, loading screens, watermarks",
                LogoVariant.IconDark => "Same as Icon but for dark backgrounds/dark mode",
                LogoVariant.HorizontalLight => "Headers, navigation bars, email signatures (light backgrounds)",
                LogoVariant.HorizontalDark => "Headers, navigation bars (dark backgrounds/dark mode)",
                LogoVariant.VerticalLight => "Splash screens, about pages, print (light backgrounds)",
                LogoVariant.VerticalDark => "Splash screens, about pages (dark backgrounds/dark mode)",
                _ => "General use"
            };
            return $"| `{l.Name}` | {l.Variant} | {usage} |";
        }));

        return new PromptContext(
            BrandName: brandName,
            Description: description,
            Logos: logos,
            Palette: palette,
            PrimaryColor: primaryColor,
            SecondaryColor: secondaryColor,
            AccentColor: accentColor,
            ExtraColors: string.Join("\n", palette.Skip(3).Select((c, i) => $"- Additional {i + 4}: {c}")),
            ExtraCssVars: string.Join("\n", palette.Skip(3).Select((c, i) => $"  --color-palette-{i + 4}: {c};")),
            LogoFileNames: string.Join(", ", logos.Select(l => $"`{l.Name}`")),
            IconRef: hasIcon ? "logo-icon.png" : "logo-base.png",
            IconDarkRef: hasIcon ? "logo-icon.png and logo-icon-dark.png" : "logo-base.png",
            HeaderLogoLight: hasHorizontal ? "logo-horizontal-light.png for light mode, logo-horizontal-dark.png for dark mode" : "logo-base.png",
            HeaderLogoDesktop: hasHorizontal ? "logo-horizontal-light.png" : "logo-base.png",
            HeaderLogoMobile: hasHorizontal ? "logo-horizontal-light.png or logo-horizontal-dark.png" : "logo-base.png",
            PaletteTable: paletteTable,
            LogoTable: logoTable);
    }

    private static List<string> BuildPrompts(PromptContext ctx)
    {
        return new List<string>
        {
            BuildReactPrompt(ctx),
            BuildDesktopPrompt(ctx),
            BuildMobilePrompt(ctx)
        };
    }

    private static string BuildColorBlock(PromptContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("COLOR PALETTE:");
        sb.AppendLine($"- Primary: {ctx.PrimaryColor}");
        sb.AppendLine($"- Secondary: {ctx.SecondaryColor}");
        sb.AppendLine($"- Accent: {ctx.AccentColor}");
        if (!string.IsNullOrEmpty(ctx.ExtraColors))
            sb.AppendLine(ctx.ExtraColors);
        sb.AppendLine();
        sb.AppendLine("LOGO FILES AVAILABLE:");
        sb.AppendLine(ctx.LogoFileNames);
        return sb.ToString();
    }

    private static string BuildReactPrompt(PromptContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"I need you to implement a complete visual layout for a React web application for the brand \"{ctx.BrandName}\".");
        sb.AppendLine();
        sb.AppendLine("BRAND DESCRIPTION:");
        sb.AppendLine(ctx.Description);
        sb.AppendLine();
        sb.Append(BuildColorBlock(ctx));
        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("1. Create a responsive layout with:");
        sb.AppendLine($"   - Header/Navbar using the horizontal logo variant ({ctx.HeaderLogoLight})");
        sb.AppendLine("   - Hero section with the brand colors as gradient or solid background");
        sb.AppendLine("   - Footer with the vertical or horizontal logo variant");
        sb.AppendLine("2. Implement a complete design system using the color palette above:");
        sb.AppendLine("   - Primary color for main CTAs, links, and active states");
        sb.AppendLine("   - Secondary color for secondary buttons, borders, and subtle backgrounds");
        sb.AppendLine("   - Accent color for highlights, badges, and notifications");
        sb.AppendLine("3. Support dark mode/light mode toggle:");
        sb.AppendLine("   - Light mode: use the \"-light\" logo variants on white/light backgrounds");
        sb.AppendLine("   - Dark mode: use the \"-dark\" logo variants on dark backgrounds");
        sb.AppendLine($"4. Use the icon variant ({ctx.IconRef}) as the favicon and PWA icon");
        sb.AppendLine("5. Typography: choose a modern sans-serif font that complements the brand");
        sb.AppendLine("6. Create reusable components: Button, Card, Input, Badge, Avatar — all using the brand colors");
        sb.AppendLine("7. Use CSS variables (custom properties) for all brand colors so they can be easily changed");
        sb.AppendLine("8. Make the layout fully responsive (mobile-first approach)");
        sb.AppendLine("9. Use Tailwind CSS or styled-components — pick whichever you prefer, but be consistent");
        sb.AppendLine("10. Include loading/splash screen with the icon logo centered on a brand-colored background");
        return sb.ToString().TrimEnd();
    }

    private static string BuildDesktopPrompt(PromptContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"I need you to implement a complete visual layout for a Windows desktop application for the brand \"{ctx.BrandName}\".");
        sb.AppendLine();
        sb.AppendLine("BRAND DESCRIPTION:");
        sb.AppendLine(ctx.Description);
        sb.AppendLine();
        sb.Append(BuildColorBlock(ctx));
        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("1. Create a professional desktop application layout with:");
        sb.AppendLine($"   - Title bar or custom chrome with the horizontal logo ({ctx.HeaderLogoDesktop})");
        sb.AppendLine("   - Sidebar navigation using the brand's primary and secondary colors");
        sb.AppendLine("   - Main content area with cards and data presentation");
        sb.AppendLine("   - Status bar at the bottom");
        sb.AppendLine("2. Implement a ResourceDictionary (XAML) with:");
        sb.AppendLine("   - All brand colors as SolidColorBrush resources");
        sb.AppendLine("   - Styles for Button, TextBox, ComboBox, ListBox, DataGrid — all themed with brand colors");
        sb.AppendLine("   - Primary color for main actions and selected states");
        sb.AppendLine("   - Secondary color for borders, panel backgrounds, and hover states");
        sb.AppendLine("   - Accent color for highlights and notifications");
        sb.AppendLine("3. Support Windows light/dark theme:");
        sb.AppendLine("   - Detect system theme and switch logo variants accordingly");
        sb.AppendLine("   - Light theme: use \"-light\" logo variants");
        sb.AppendLine("   - Dark theme: use \"-dark\" logo variants");
        sb.AppendLine($"4. Window icon: use the icon variant ({ctx.IconRef}) as the .ico file");
        sb.AppendLine("5. Splash screen on startup: centered icon logo on a background with the primary color");
        sb.AppendLine("6. Use the recommended WinUI 3 or WPF framework");
        sb.AppendLine("7. Include a consistent margin/padding system (8px base grid)");
        sb.AppendLine("8. Navigation should support: sidebar collapse, breadcrumbs, and tab-based content");
        return sb.ToString().TrimEnd();
    }

    private static string BuildMobilePrompt(PromptContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"I need you to implement a complete visual layout for a mobile application for the brand \"{ctx.BrandName}\".");
        sb.AppendLine();
        sb.AppendLine("BRAND DESCRIPTION:");
        sb.AppendLine(ctx.Description);
        sb.AppendLine();
        sb.Append(BuildColorBlock(ctx));
        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("1. Create a mobile-first layout with:");
        sb.AppendLine($"   - Splash/launch screen with centered icon logo ({ctx.IconRef}) on a primary-colored background");
        sb.AppendLine($"   - Top app bar / navigation bar with the horizontal logo ({ctx.HeaderLogoMobile})");
        sb.AppendLine("   - Bottom tab navigation with icons using the brand's primary color for active state");
        sb.AppendLine("   - Pull-to-refresh with brand-colored indicator");
        sb.AppendLine("2. Implement a complete theme/design system:");
        sb.AppendLine("   - Primary color for main CTAs, FABs, selected tabs, and active states");
        sb.AppendLine("   - Secondary color for secondary actions, card borders, and input outlines");
        sb.AppendLine("   - Accent color for badges, notifications, and small highlights");
        sb.AppendLine("3. Support light mode and dark mode:");
        sb.AppendLine("   - Light mode: white/light backgrounds, dark text, \"-light\" logo variants");
        sb.AppendLine("   - Dark mode: dark backgrounds, light text, \"-dark\" logo variants");
        sb.AppendLine("   - Follow system preference by default, with manual toggle option");
        sb.AppendLine($"4. App icon: use the icon variant ({ctx.IconDarkRef}) to generate adaptive icons (Android) and app icons (iOS)");
        sb.AppendLine("5. Create reusable themed components: Button (primary/secondary/outline), Card, Input, Avatar, Badge, Chip, Dialog");
        sb.AppendLine("6. Typography: use a system font or a modern sans-serif that complements the brand");
        sb.AppendLine("7. Consistent spacing system (4px or 8px base unit)");
        sb.AppendLine("8. Animations: subtle fade/slide transitions using the brand's accent color for loading indicators");
        sb.AppendLine("9. Include onboarding screens (2-3 slides) showcasing the brand with the vertical logo variant");
        sb.AppendLine("10. Handle safe areas, notch, and different screen sizes properly");
        return sb.ToString().TrimEnd();
    }

    private static string BuildMarkdown(PromptContext ctx, List<string> prompts)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"# {ctx.BrandName} — Brand Implementation Guide");
        sb.AppendLine();
        sb.AppendLine("## Brand Overview");
        sb.AppendLine();
        sb.AppendLine($"- **Brand Name:** {ctx.BrandName}");
        sb.AppendLine($"- **Description:** {ctx.Description}");
        sb.AppendLine();
        sb.AppendLine("## Color Palette");
        sb.AppendLine();
        sb.AppendLine("| Role | Hex Code | Preview |");
        sb.AppendLine("|------|----------|---------|");
        sb.AppendLine(ctx.PaletteTable);
        sb.AppendLine();
        sb.AppendLine("### CSS Variables");
        sb.AppendLine();
        sb.AppendLine("```css");
        sb.AppendLine(":root {");
        sb.AppendLine($"  --color-primary: {ctx.PrimaryColor};");
        sb.AppendLine($"  --color-secondary: {ctx.SecondaryColor};");
        sb.AppendLine($"  --color-accent: {ctx.AccentColor};");
        if (!string.IsNullOrEmpty(ctx.ExtraCssVars))
            sb.AppendLine(ctx.ExtraCssVars);
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Logo Assets");
        sb.AppendLine();
        sb.AppendLine("| File | Variant | Recommended Usage |");
        sb.AppendLine("|------|---------|-------------------|");
        sb.AppendLine(ctx.LogoTable);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## AI Implementation Prompts");
        sb.AppendLine();
        sb.AppendLine("Use the prompts below with an AI assistant to implement the brand's visual identity in your project.");
        sb.AppendLine("Each prompt is also saved as an individual file in the `prompts/` folder for easy copy-paste.");
        sb.AppendLine();
        sb.AppendLine($"- `prompts/{PromptFileNames[0]}` — Frontend React (Web)");
        sb.AppendLine($"- `prompts/{PromptFileNames[1]}` — Desktop Application (Windows — WPF/WinUI)");
        sb.AppendLine($"- `prompts/{PromptFileNames[2]}` — Mobile Application (React Native / Flutter)");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        var titles = new[] { "Frontend React (Web)", "Desktop Application (Windows — WPF/WinUI)", "Mobile Application (React Native / Flutter)" };
        for (var i = 0; i < prompts.Count; i++)
        {
            sb.AppendLine($"### Prompt {i + 1}: {titles[i]}");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(prompts[i]);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine("> Generated by **AI Logo Maker** — Use these prompts with any AI coding assistant (Claude, ChatGPT, Copilot, etc.) to implement your brand's visual identity consistently across platforms.");

        return sb.ToString();
    }
}
