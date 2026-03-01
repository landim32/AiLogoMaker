namespace AiLogoMaker.Domain.Models;

public static class ExportPresets
{
    public static List<ExportConfig> GetAndroidIcons(string baseDir)
    {
        return
        [
            new() { Name = "mipmap-mdpi", Width = 48, Height = 48, OutputPath = Path.Combine(baseDir, "android", "mipmap-mdpi", "ic_launcher.png") },
            new() { Name = "mipmap-hdpi", Width = 72, Height = 72, OutputPath = Path.Combine(baseDir, "android", "mipmap-hdpi", "ic_launcher.png") },
            new() { Name = "mipmap-xhdpi", Width = 96, Height = 96, OutputPath = Path.Combine(baseDir, "android", "mipmap-xhdpi", "ic_launcher.png") },
            new() { Name = "mipmap-xxhdpi", Width = 144, Height = 144, OutputPath = Path.Combine(baseDir, "android", "mipmap-xxhdpi", "ic_launcher.png") },
            new() { Name = "mipmap-xxxhdpi", Width = 192, Height = 192, OutputPath = Path.Combine(baseDir, "android", "mipmap-xxxhdpi", "ic_launcher.png") },
            new() { Name = "play-store", Width = 512, Height = 512, OutputPath = Path.Combine(baseDir, "android", "play-store-icon.png") },
        ];
    }

    public static List<ExportConfig> GetAndroidAdaptiveIcons(string baseDir)
    {
        return
        [
            new() { Name = "adaptive-mdpi", Width = 108, Height = 108, OutputPath = Path.Combine(baseDir, "android", "mipmap-mdpi", "ic_launcher_foreground.png") },
            new() { Name = "adaptive-hdpi", Width = 162, Height = 162, OutputPath = Path.Combine(baseDir, "android", "mipmap-hdpi", "ic_launcher_foreground.png") },
            new() { Name = "adaptive-xhdpi", Width = 216, Height = 216, OutputPath = Path.Combine(baseDir, "android", "mipmap-xhdpi", "ic_launcher_foreground.png") },
            new() { Name = "adaptive-xxhdpi", Width = 324, Height = 324, OutputPath = Path.Combine(baseDir, "android", "mipmap-xxhdpi", "ic_launcher_foreground.png") },
            new() { Name = "adaptive-xxxhdpi", Width = 432, Height = 432, OutputPath = Path.Combine(baseDir, "android", "mipmap-xxxhdpi", "ic_launcher_foreground.png") },
        ];
    }

    public static List<ExportConfig> GetAndroidSplash(string baseDir, bool isDark)
    {
        var suffix = isDark ? "dark" : "light";
        var dir = Path.Combine(baseDir, "android", "splash", suffix);
        return
        [
            new() { Name = $"splash-mdpi-{suffix}", Width = 480, Height = 320, OutputPath = Path.Combine(dir, "splash-mdpi.png") },
            new() { Name = $"splash-hdpi-{suffix}", Width = 800, Height = 480, OutputPath = Path.Combine(dir, "splash-hdpi.png") },
            new() { Name = $"splash-xhdpi-{suffix}", Width = 1280, Height = 720, OutputPath = Path.Combine(dir, "splash-xhdpi.png") },
            new() { Name = $"splash-xxhdpi-{suffix}", Width = 1920, Height = 1080, OutputPath = Path.Combine(dir, "splash-xxhdpi.png") },
            new() { Name = $"splash-xxxhdpi-{suffix}", Width = 2560, Height = 1440, OutputPath = Path.Combine(dir, "splash-xxxhdpi.png") },
        ];
    }

    public static List<ExportConfig> GetIosIcons(string baseDir)
    {
        var dir = Path.Combine(baseDir, "ios", "AppIcon.appiconset");
        return
        [
            new() { Name = "ios-1024", Width = 1024, Height = 1024, OutputPath = Path.Combine(dir, "icon-1024.png") },
            new() { Name = "ios-180", Width = 180, Height = 180, OutputPath = Path.Combine(dir, "icon-180.png") },
            new() { Name = "ios-167", Width = 167, Height = 167, OutputPath = Path.Combine(dir, "icon-167.png") },
            new() { Name = "ios-152", Width = 152, Height = 152, OutputPath = Path.Combine(dir, "icon-152.png") },
            new() { Name = "ios-120", Width = 120, Height = 120, OutputPath = Path.Combine(dir, "icon-120.png") },
            new() { Name = "ios-87", Width = 87, Height = 87, OutputPath = Path.Combine(dir, "icon-87.png") },
            new() { Name = "ios-80", Width = 80, Height = 80, OutputPath = Path.Combine(dir, "icon-80.png") },
            new() { Name = "ios-76", Width = 76, Height = 76, OutputPath = Path.Combine(dir, "icon-76.png") },
            new() { Name = "ios-60", Width = 60, Height = 60, OutputPath = Path.Combine(dir, "icon-60.png") },
            new() { Name = "ios-58", Width = 58, Height = 58, OutputPath = Path.Combine(dir, "icon-58.png") },
            new() { Name = "ios-40", Width = 40, Height = 40, OutputPath = Path.Combine(dir, "icon-40.png") },
            new() { Name = "ios-29", Width = 29, Height = 29, OutputPath = Path.Combine(dir, "icon-29.png") },
            new() { Name = "ios-20", Width = 20, Height = 20, OutputPath = Path.Combine(dir, "icon-20.png") },
        ];
    }

    public static List<ExportConfig> GetIosSplash(string baseDir, bool isDark)
    {
        var suffix = isDark ? "dark" : "light";
        var dir = Path.Combine(baseDir, "ios", "splash", suffix);
        return
        [
            new() { Name = $"ios-splash-1242x2688-{suffix}", Width = 1242, Height = 2688, OutputPath = Path.Combine(dir, "splash-1242x2688.png") },
            new() { Name = $"ios-splash-1125x2436-{suffix}", Width = 1125, Height = 2436, OutputPath = Path.Combine(dir, "splash-1125x2436.png") },
            new() { Name = $"ios-splash-828x1792-{suffix}", Width = 828, Height = 1792, OutputPath = Path.Combine(dir, "splash-828x1792.png") },
            new() { Name = $"ios-splash-1242x2208-{suffix}", Width = 1242, Height = 2208, OutputPath = Path.Combine(dir, "splash-1242x2208.png") },
            new() { Name = $"ios-splash-750x1334-{suffix}", Width = 750, Height = 1334, OutputPath = Path.Combine(dir, "splash-750x1334.png") },
            new() { Name = $"ios-splash-640x1136-{suffix}", Width = 640, Height = 1136, OutputPath = Path.Combine(dir, "splash-640x1136.png") },
            new() { Name = $"ios-splash-2048x2732-{suffix}", Width = 2048, Height = 2732, OutputPath = Path.Combine(dir, "splash-2048x2732.png") },
            new() { Name = $"ios-splash-1668x2388-{suffix}", Width = 1668, Height = 2388, OutputPath = Path.Combine(dir, "splash-1668x2388.png") },
            new() { Name = $"ios-splash-1668x2224-{suffix}", Width = 1668, Height = 2224, OutputPath = Path.Combine(dir, "splash-1668x2224.png") },
            new() { Name = $"ios-splash-1536x2048-{suffix}", Width = 1536, Height = 2048, OutputPath = Path.Combine(dir, "splash-1536x2048.png") },
        ];
    }

    public static List<ExportConfig> GetFavicons(string baseDir)
    {
        var dir = Path.Combine(baseDir, "favicon");
        return
        [
            new() { Name = "favicon-16", Width = 16, Height = 16, OutputPath = Path.Combine(dir, "favicon-16x16.png") },
            new() { Name = "favicon-32", Width = 32, Height = 32, OutputPath = Path.Combine(dir, "favicon-32x32.png") },
            new() { Name = "favicon-48", Width = 48, Height = 48, OutputPath = Path.Combine(dir, "favicon-48x48.png") },
            new() { Name = "favicon-64", Width = 64, Height = 64, OutputPath = Path.Combine(dir, "favicon-64x64.png") },
            new() { Name = "favicon-96", Width = 96, Height = 96, OutputPath = Path.Combine(dir, "favicon-96x96.png") },
            new() { Name = "favicon-128", Width = 128, Height = 128, OutputPath = Path.Combine(dir, "favicon-128x128.png") },
            new() { Name = "favicon-144", Width = 144, Height = 144, OutputPath = Path.Combine(dir, "favicon-144x144.png") },
            new() { Name = "favicon-192", Width = 192, Height = 192, OutputPath = Path.Combine(dir, "favicon-192x192.png") },
            new() { Name = "favicon-256", Width = 256, Height = 256, OutputPath = Path.Combine(dir, "favicon-256x256.png") },
            new() { Name = "favicon-512", Width = 512, Height = 512, OutputPath = Path.Combine(dir, "favicon-512x512.png") },
            new() { Name = "apple-touch-icon", Width = 180, Height = 180, OutputPath = Path.Combine(dir, "apple-touch-icon.png") },
        ];
    }

    public static List<ExportConfig> GetSocialMedia(string baseDir)
    {
        var dir = Path.Combine(baseDir, "social");
        return
        [
            new() { Name = "og-image", Width = 1200, Height = 630, OutputPath = Path.Combine(dir, "og-image.png") },
            new() { Name = "twitter-card", Width = 1200, Height = 600, OutputPath = Path.Combine(dir, "twitter-card.png") },
        ];
    }
}
