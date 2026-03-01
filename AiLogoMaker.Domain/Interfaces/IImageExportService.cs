using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Interfaces;

public class ExportSummary
{
    public required List<ExportCategoryInfo> Categories { get; set; }
    public int TotalFiles { get; set; }
    public required string OutputPath { get; set; }
}

public class ExportCategoryInfo
{
    public required string Name { get; set; }
    public int FileCount { get; set; }
    public bool Generated { get; set; }
}

public interface IImageExportService
{
    Task ExportAllAsync(List<LogoResult> logos, string outputDir);
    ExportSummary GetExportSummary(string outputDir);
}
