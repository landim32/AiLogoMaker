using System.Text.Json;
using AiLogoMaker.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AiLogoMaker.Services;

public class ImageExportService
{
    public async Task ExportAllAsync(List<LogoResult> logos, string outputDir)
    {
        var squareLogo = logos.FirstOrDefault(l => l.Variant == LogoVariant.Square);
        var horizontalLight = logos.FirstOrDefault(l => l.Variant == LogoVariant.HorizontalLight);
        var horizontalDark = logos.FirstOrDefault(l => l.Variant == LogoVariant.HorizontalDark);
        var verticalLight = logos.FirstOrDefault(l => l.Variant == LogoVariant.VerticalLight);
        var verticalDark = logos.FirstOrDefault(l => l.Variant == LogoVariant.VerticalDark);

        if (squareLogo == null)
        {
            Console.WriteLine("  ERROR: Square logo not found. Required for export.");
            return;
        }

        Console.WriteLine("\n  Exporting Android icons...");
        await ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetAndroidIcons(outputDir));

        Console.WriteLine("  Exporting Android adaptive icons...");
        await ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetAndroidAdaptiveIcons(outputDir));

        Console.WriteLine("  Exporting Android splash screens...");
        if (horizontalLight != null)
            await ExportSplashAsync(squareLogo.FilePath, ExportPresets.GetAndroidSplash(outputDir, isDark: false), new Rgba32(255, 255, 255, 255));
        if (horizontalDark != null)
            await ExportSplashAsync(squareLogo.FilePath, ExportPresets.GetAndroidSplash(outputDir, isDark: true), new Rgba32(18, 18, 18, 255));

        Console.WriteLine("  Exporting iOS icons...");
        await ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetIosIcons(outputDir));
        await GenerateContentsJsonAsync(outputDir);

        Console.WriteLine("  Exporting iOS splash screens...");
        if (verticalLight != null)
            await ExportSplashAsync(squareLogo.FilePath, ExportPresets.GetIosSplash(outputDir, isDark: false), new Rgba32(255, 255, 255, 255));
        if (verticalDark != null)
            await ExportSplashAsync(squareLogo.FilePath, ExportPresets.GetIosSplash(outputDir, isDark: true), new Rgba32(18, 18, 18, 255));

        Console.WriteLine("  Exporting favicons...");
        await ExportResizedAsync(squareLogo.FilePath, ExportPresets.GetFavicons(outputDir));

        Console.WriteLine("  Exporting social media images...");
        await ExportSocialMediaAsync(squareLogo.FilePath, horizontalLight?.FilePath, ExportPresets.GetSocialMedia(outputDir));
    }

    private static async Task ExportResizedAsync(string sourcePath, List<ExportConfig> configs)
    {
        using var source = await Image.LoadAsync<Rgba32>(sourcePath);

        foreach (var config in configs)
        {
            var dir = Path.GetDirectoryName(config.OutputPath)!;
            Directory.CreateDirectory(dir);

            using var resized = source.Clone(ctx =>
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(config.Width, config.Height),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));

            // Center on transparent canvas of exact size
            using var canvas = new Image<Rgba32>(config.Width, config.Height);
            var x = (config.Width - resized.Width) / 2;
            var y = (config.Height - resized.Height) / 2;
            canvas.Mutate(ctx => ctx.DrawImage(resized, new Point(x, y), 1f));

            await canvas.SaveAsPngAsync(config.OutputPath, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
        }
    }

    private static async Task ExportSplashAsync(string logoPath, List<ExportConfig> configs, Rgba32 bgColor)
    {
        using var logo = await Image.LoadAsync<Rgba32>(logoPath);

        foreach (var config in configs)
        {
            var dir = Path.GetDirectoryName(config.OutputPath)!;
            Directory.CreateDirectory(dir);

            using var canvas = new Image<Rgba32>(config.Width, config.Height, bgColor);

            // Logo occupies ~30% of the smallest dimension
            var maxLogoSize = (int)(Math.Min(config.Width, config.Height) * 0.3);
            using var resizedLogo = logo.Clone(ctx =>
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxLogoSize, maxLogoSize),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));

            var x = (config.Width - resizedLogo.Width) / 2;
            var y = (config.Height - resizedLogo.Height) / 2;
            canvas.Mutate(ctx => ctx.DrawImage(resizedLogo, new Point(x, y), 1f));

            await canvas.SaveAsPngAsync(config.OutputPath, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
        }
    }

    private static async Task ExportSocialMediaAsync(string squareLogoPath, string? horizontalLogoPath, List<ExportConfig> configs)
    {
        var logoPath = horizontalLogoPath ?? squareLogoPath;
        using var logo = await Image.LoadAsync<Rgba32>(logoPath);

        foreach (var config in configs)
        {
            var dir = Path.GetDirectoryName(config.OutputPath)!;
            Directory.CreateDirectory(dir);

            using var canvas = new Image<Rgba32>(config.Width, config.Height, new Rgba32(255, 255, 255, 255));

            var maxLogoWidth = (int)(config.Width * 0.6);
            var maxLogoHeight = (int)(config.Height * 0.5);
            using var resizedLogo = logo.Clone(ctx =>
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxLogoWidth, maxLogoHeight),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));

            var x = (config.Width - resizedLogo.Width) / 2;
            var y = (config.Height - resizedLogo.Height) / 2;
            canvas.Mutate(ctx => ctx.DrawImage(resizedLogo, new Point(x, y), 1f));

            await canvas.SaveAsPngAsync(config.OutputPath, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
        }
    }

    private static async Task GenerateContentsJsonAsync(string outputDir)
    {
        var appIconDir = Path.Combine(outputDir, "ios", "AppIcon.appiconset");
        Directory.CreateDirectory(appIconDir);

        var contents = new
        {
            images = new object[]
            {
                new { size = "20x20", idiom = "iphone", filename = "icon-40.png", scale = "2x" },
                new { size = "20x20", idiom = "iphone", filename = "icon-60.png", scale = "3x" },
                new { size = "29x29", idiom = "iphone", filename = "icon-58.png", scale = "2x" },
                new { size = "29x29", idiom = "iphone", filename = "icon-87.png", scale = "3x" },
                new { size = "40x40", idiom = "iphone", filename = "icon-80.png", scale = "2x" },
                new { size = "40x40", idiom = "iphone", filename = "icon-120.png", scale = "3x" },
                new { size = "60x60", idiom = "iphone", filename = "icon-120.png", scale = "2x" },
                new { size = "60x60", idiom = "iphone", filename = "icon-180.png", scale = "3x" },
                new { size = "20x20", idiom = "ipad", filename = "icon-20.png", scale = "1x" },
                new { size = "20x20", idiom = "ipad", filename = "icon-40.png", scale = "2x" },
                new { size = "29x29", idiom = "ipad", filename = "icon-29.png", scale = "1x" },
                new { size = "29x29", idiom = "ipad", filename = "icon-58.png", scale = "2x" },
                new { size = "40x40", idiom = "ipad", filename = "icon-40.png", scale = "1x" },
                new { size = "40x40", idiom = "ipad", filename = "icon-80.png", scale = "2x" },
                new { size = "76x76", idiom = "ipad", filename = "icon-76.png", scale = "1x" },
                new { size = "76x76", idiom = "ipad", filename = "icon-152.png", scale = "2x" },
                new { size = "83.5x83.5", idiom = "ipad", filename = "icon-167.png", scale = "2x" },
                new { size = "1024x1024", idiom = "ios-marketing", filename = "icon-1024.png", scale = "1x" }
            },
            info = new { version = 1, author = "AiLogoMaker" }
        };

        var json = JsonSerializer.Serialize(contents, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(appIconDir, "Contents.json"), json);
    }

    public static void PrintExportSummary(string outputDir)
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine("  EXPORT SUMMARY");
        Console.WriteLine("========================================\n");

        var categories = new[]
        {
            ("Originals (5 versions)", Path.Combine(outputDir, "originals")),
            ("Android Icons", Path.Combine(outputDir, "android")),
            ("Android Splash", Path.Combine(outputDir, "android", "splash")),
            ("iOS Icons", Path.Combine(outputDir, "ios", "AppIcon.appiconset")),
            ("iOS Splash", Path.Combine(outputDir, "ios", "splash")),
            ("Favicons", Path.Combine(outputDir, "favicon")),
            ("Social Media", Path.Combine(outputDir, "social")),
        };

        var totalFiles = 0;
        foreach (var (name, dir) in categories)
        {
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories);
                var jsonFiles = Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
                var count = files.Length + jsonFiles.Length;
                totalFiles += count;
                Console.WriteLine($"  {name}: {count} file(s)");
            }
            else
            {
                Console.WriteLine($"  {name}: (not generated)");
            }
        }

        Console.WriteLine($"\n  TOTAL: {totalFiles} file(s)");
        Console.WriteLine($"  Output: {Path.GetFullPath(outputDir)}");
        Console.WriteLine();
    }
}
