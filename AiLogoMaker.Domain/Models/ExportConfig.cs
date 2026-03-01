namespace AiLogoMaker.Domain.Models;

public class ExportConfig
{
    public required string Name { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required string OutputPath { get; set; }
}
