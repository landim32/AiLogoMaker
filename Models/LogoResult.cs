namespace AiLogoMaker.Models;

public class LogoResult
{
    public required string Name { get; set; }
    public required string FilePath { get; set; }
    public required LogoVariant Variant { get; set; }
    public required string Prompt { get; set; }
}

public enum LogoVariant
{
    Square,
    HorizontalLight,
    HorizontalDark,
    VerticalLight,
    VerticalDark
}
