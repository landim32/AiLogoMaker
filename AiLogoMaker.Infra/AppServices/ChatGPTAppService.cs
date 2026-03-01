using System.ClientModel;
using AiLogoMaker.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI.Images;

namespace AiLogoMaker.Infra.AppServices;

public class ChatGPTAppService : IAIAppService
{
    private readonly ImageClient _client;
    private readonly ILogger<ChatGPTAppService> _logger;
    private static readonly HttpClient s_httpClient = new();

    public ChatGPTAppService(string apiKey, ILogger<ChatGPTAppService> logger)
    {
        _client = new ImageClient("gpt-image-1", new ApiKeyCredential(apiKey));
        _logger = logger;
    }

    public async Task<byte[]> GenerateImageAsync(string prompt, string size)
    {
        var parsedSize = size switch
        {
            "1024x1024" => GeneratedImageSize.W1024xH1024,
            "1536x1024" => GeneratedImageSize.W1536xH1024,
            "1024x1536" => GeneratedImageSize.W1024xH1536,
            _ => GeneratedImageSize.W1024xH1024
        };

        var options = new ImageGenerationOptions
        {
            Size = parsedSize,
            Quality = new GeneratedImageQuality("high"),
            OutputFileFormat = new GeneratedImageFileFormat("png")
        };

        _logger.LogInformation("Calling OpenAI image generation ({Size})...", size);

        var result = await _client.GenerateImageAsync(prompt, options);
        var image = result.Value;

        if (image.ImageBytes != null && image.ImageBytes.Length > 0)
        {
            return image.ImageBytes.ToArray();
        }

        if (image.ImageUri != null)
        {
            return await s_httpClient.GetByteArrayAsync(image.ImageUri);
        }

        throw new InvalidOperationException("A API nao retornou imagem (nem bytes nem URL).");
    }

    public async Task<byte[]> EditImageAsync(string sourceImagePath, string prompt, string size)
    {
        var parsedSize = size switch
        {
            "1024x1024" => GeneratedImageSize.W1024xH1024,
            "1536x1024" => GeneratedImageSize.W1536xH1024,
            "1024x1536" => GeneratedImageSize.W1024xH1536,
            _ => GeneratedImageSize.W1024xH1024
        };

        var options = new ImageEditOptions
        {
            Size = parsedSize,
            OutputFileFormat = new GeneratedImageFileFormat("png"),
            Background = new GeneratedImageBackground("transparent")
        };

        _logger.LogInformation("Calling OpenAI image edit ({Size}) based on {Source}...", size, Path.GetFileName(sourceImagePath));

        var result = await _client.GenerateImageEditAsync(sourceImagePath, prompt, options);
        var image = result.Value;

        if (image.ImageBytes != null && image.ImageBytes.Length > 0)
        {
            return image.ImageBytes.ToArray();
        }

        if (image.ImageUri != null)
        {
            return await s_httpClient.GetByteArrayAsync(image.ImageUri);
        }

        throw new InvalidOperationException("A API nao retornou imagem (nem bytes nem URL).");
    }
}
