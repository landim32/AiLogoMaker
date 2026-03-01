namespace AiLogoMaker.Domain.Interfaces;

public interface IAIAppService
{
    Task<byte[]> GenerateImageAsync(string prompt, string size);
    Task<byte[]> EditImageAsync(string sourceImagePath, string prompt, string size);
}
