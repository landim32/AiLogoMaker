using AiLogoMaker.Domain.Interfaces;

namespace AiLogoMaker.Infra.Repositories;

public class FileSystemPromptRepository : IPromptRepository
{
    private readonly string _promptsDir;

    public FileSystemPromptRepository(string promptsDir)
    {
        _promptsDir = promptsDir;
    }

    public string? LoadPromptContent(string promptName)
    {
        var path = Path.Combine(_promptsDir, $"{promptName}.md");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public List<string> GetAvailableStyles()
    {
        var styles = new List<string> { "wordmark", "lettermark", "brandmark", "combined", "emblem", "mascot" };
        return styles.Where(s => File.Exists(Path.Combine(_promptsDir, $"{s}.md"))).ToList();
    }

    public List<string> GetAvailableRules()
    {
        var rules = new List<string>
        {
            "rules-circular-grid",
            "rules-minimalism",
            "rules-golden-ratio",
            "rules-modular-grid",
            "rules-flat-design",
            "rules-responsive-logo"
        };
        return rules.Where(r => File.Exists(Path.Combine(_promptsDir, $"{r}.md"))).ToList();
    }
}
