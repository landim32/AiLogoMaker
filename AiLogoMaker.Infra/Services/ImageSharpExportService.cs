using System.Text.Json;
using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;
using AiLogoMaker.Domain.Services.Export;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AiLogoMaker.Infra.Services;

public class ImageSharpExportService : IImageExportService, IImageResizeService
{
    private readonly ILogger<ImageSharpExportService> _logger;

    public ImageSharpExportService(ILogger<ImageSharpExportService> logger)
    {
        _logger = logger;
    }

    public async Task ExportAllAsync(List<LogoResult> logos, string outputDir)
    {
        var androidService = new AndroidExportService(this);
        var iosService = new IosExportService(this);
        var faviconService = new FaviconExportService(this);
        var socialMediaService = new SocialMediaExportService(this);

        _logger.LogInformation("Exporting Android assets...");
        await androidService.ExportAsync(logos, outputDir);

        _logger.LogInformation("Exporting iOS assets...");
        await iosService.ExportAsync(logos, outputDir);

        _logger.LogInformation("Exporting favicons...");
        await faviconService.ExportAsync(logos, outputDir);

        _logger.LogInformation("Exporting social media images...");
        await socialMediaService.ExportAsync(logos, outputDir);
    }

    public ExportSummary GetExportSummary(string outputDir)
    {
        var categories = new (string Name, string Dir)[]
        {
            ("Originals (5 versions)", Path.Combine(outputDir, "originals")),
            ("Android Icons", Path.Combine(outputDir, "android")),
            ("Android Splash", Path.Combine(outputDir, "android", "splash")),
            ("iOS Icons", Path.Combine(outputDir, "ios", "AppIcon.appiconset")),
            ("iOS Splash", Path.Combine(outputDir, "ios", "splash")),
            ("Favicons", Path.Combine(outputDir, "favicon")),
            ("Social Media", Path.Combine(outputDir, "social")),
        };

        var result = new ExportSummary
        {
            Categories = new List<ExportCategoryInfo>(),
            TotalFiles = 0,
            OutputPath = Path.GetFullPath(outputDir)
        };

        foreach (var (name, dir) in categories)
        {
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories);
                var jsonFiles = Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
                var count = files.Length + jsonFiles.Length;
                result.TotalFiles += count;
                result.Categories.Add(new ExportCategoryInfo { Name = name, FileCount = count, Generated = true });
            }
            else
            {
                result.Categories.Add(new ExportCategoryInfo { Name = name, FileCount = 0, Generated = false });
            }
        }

        return result;
    }

    public async Task ExportResizedAsync(string sourcePath, List<ExportConfig> configs)
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

            using var canvas = new Image<Rgba32>(config.Width, config.Height);
            var x = (config.Width - resized.Width) / 2;
            var y = (config.Height - resized.Height) / 2;
            canvas.Mutate(ctx => ctx.DrawImage(resized, new Point(x, y), 1f));

            await canvas.SaveAsPngAsync(config.OutputPath, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
        }
    }

    public async Task ExportSplashAsync(string logoPath, List<ExportConfig> configs, bool isDark)
    {
        var bgColor = isDark ? new Rgba32(18, 18, 18, 255) : new Rgba32(255, 255, 255, 255);
        using var logo = await Image.LoadAsync<Rgba32>(logoPath);

        foreach (var config in configs)
        {
            var dir = Path.GetDirectoryName(config.OutputPath)!;
            Directory.CreateDirectory(dir);

            using var canvas = new Image<Rgba32>(config.Width, config.Height, bgColor);

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

    public async Task ExportSocialMediaAsync(string squareLogoPath, string? horizontalLogoPath, List<ExportConfig> configs)
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

    public async Task GenerateContentsJsonAsync(string outputDir)
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
}
