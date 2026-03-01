namespace AiLogoMaker.Domain.Interfaces;

public interface IPromptRepository
{
    string? LoadPromptContent(string promptName);
    List<string> GetAvailableStyles();
    List<string> GetAvailableRules();
}
