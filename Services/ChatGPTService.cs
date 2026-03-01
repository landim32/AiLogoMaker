using System.ClientModel;
using AiLogoMaker.Models;
using OpenAI.Images;

namespace AiLogoMaker.Services;

public class ChatGPTService
{
    private readonly ImageClient _client;
    private readonly string _promptsDir;

    public ChatGPTService(string apiKey, string promptsDir)
    {
        _client = new ImageClient("gpt-image-1", new ApiKeyCredential(apiKey));
        _promptsDir = promptsDir;
    }

    public async Task<List<LogoResult>> GenerateLogosAsync(
        string userPrompt,
        string brandName,
        string logoStyle,
        List<string> designRules,
        string outputDir)
    {
        var results = new List<LogoResult>();
        var originalsDir = Path.Combine(outputDir, "originals");
        Directory.CreateDirectory(originalsDir);

        var basePrompt = BuildBasePrompt(userPrompt, brandName, logoStyle, designRules);

        var variants = new[]
        {
            (LogoVariant.Square, "logo-square.png", "1024x1024", BuildSquarePrompt(basePrompt, brandName)),
            (LogoVariant.HorizontalLight, "logo-horizontal-light.png", "1536x1024", BuildHorizontalPrompt(basePrompt, brandName, isForDarkBg: false)),
            (LogoVariant.HorizontalDark, "logo-horizontal-dark.png", "1536x1024", BuildHorizontalPrompt(basePrompt, brandName, isForDarkBg: true)),
            (LogoVariant.VerticalLight, "logo-vertical-light.png", "1024x1536", BuildVerticalPrompt(basePrompt, brandName, isForDarkBg: false)),
            (LogoVariant.VerticalDark, "logo-vertical-dark.png", "1024x1536", BuildVerticalPrompt(basePrompt, brandName, isForDarkBg: true)),
        };

        foreach (var (variant, fileName, size, prompt) in variants)
        {
            var filePath = Path.Combine(originalsDir, fileName);
            Console.WriteLine();
            Console.WriteLine($"  Generating: {fileName}...");

            try
            {
                var imageBytes = await GenerateImageAsync(prompt, size);
                await File.WriteAllBytesAsync(filePath, imageBytes);

                results.Add(new LogoResult
                {
                    Name = fileName,
                    FilePath = filePath,
                    Variant = variant,
                    Prompt = prompt
                });

                Console.WriteLine($"  OK: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR generating {fileName}: {ex.Message}");
            }
        }

        return results;
    }

    private static readonly HttpClient s_httpClient = new();

    private async Task<byte[]> GenerateImageAsync(string prompt, string size)
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
            Quality = GeneratedImageQuality.High
        };

        var result = await _client.GenerateImageAsync(prompt, options);
        var image = result.Value;

        // gpt-image-1 returns base64 bytes directly; DALL-E models return a URL
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

    private string BuildBasePrompt(string userPrompt, string brandName, string logoStyle, List<string> designRules)
    {
        var parts = new List<string>();

        // User prompt
        parts.Add($"Briefing do cliente: {userPrompt}");
        parts.Add($"Nome da marca: {brandName}");

        // Logo style from markdown
        var stylePath = Path.Combine(_promptsDir, $"{logoStyle}.md");
        if (File.Exists(stylePath))
        {
            var styleContent = File.ReadAllText(stylePath);
            parts.Add($"Estilo de logo:\n{styleContent}");
        }

        // Design rules from markdown
        foreach (var rule in designRules)
        {
            var rulePath = Path.Combine(_promptsDir, $"{rule}.md");
            if (File.Exists(rulePath))
            {
                var ruleContent = File.ReadAllText(rulePath);
                parts.Add($"Regra de design:\n{ruleContent}");
            }
        }

        // Color study
        var colorPath = Path.Combine(_promptsDir, "estudo-de-cores.md");
        if (File.Exists(colorPath))
        {
            var colorContent = File.ReadAllText(colorPath);
            parts.Add($"Estudo de cores:\n{colorContent}");
        }

        return string.Join("\n\n---\n\n", parts);
    }

    private static string BuildSquarePrompt(string basePrompt, string brandName)
    {
        return $"""
            Crie um logotipo profissional QUADRADO (proporção 1:1) para a marca "{brandName}".

            REQUISITOS OBRIGATÓRIOS:
            - Formato quadrado, proporção 1:1
            - Fundo TRANSPARENTE (sem fundo)
            - Apenas o símbolo/ícone da marca, SEM texto
            - Design limpo, vetorial, profissional
            - Cores vibrantes e bem definidas
            - O ícone deve ser centralizado e ocupar bem o espaço
            - Deve funcionar como app icon e favicon
            - Sem mockups, sem sombras no fundo, sem bordas decorativas

            {basePrompt}
            """;
    }

    private static string BuildHorizontalPrompt(string basePrompt, string brandName, bool isForDarkBg)
    {
        var colorScheme = isForDarkBg
            ? "Versão para FUNDO ESCURO: use cores claras e vibrantes para o texto e o ícone. O texto deve ser branco ou em cor clara que contraste com fundos escuros/pretos."
            : "Versão para FUNDO CLARO: use cores escuras e saturadas para o texto. O texto deve ser preto ou em cor escura que contraste com fundos claros/brancos.";

        return $"""
            Crie um logotipo profissional HORIZONTAL (proporção 3:2, paisagem) para a marca "{brandName}".

            REQUISITOS OBRIGATÓRIOS:
            - Formato horizontal/paisagem, proporção 3:2
            - Fundo TRANSPARENTE (sem fundo)
            - Ícone/símbolo à ESQUERDA e nome "{brandName}" à DIREITA
            - Layout horizontal com elementos alinhados
            - Design limpo, vetorial, profissional
            - O nome deve ser claramente legível
            - Tipografia elegante e moderna
            - Sem mockups, sem sombras no fundo, sem bordas decorativas

            {colorScheme}

            {basePrompt}
            """;
    }

    private static string BuildVerticalPrompt(string basePrompt, string brandName, bool isForDarkBg)
    {
        var colorScheme = isForDarkBg
            ? "Versão para FUNDO ESCURO: use cores claras e vibrantes para o texto e o ícone. O texto deve ser branco ou em cor clara que contraste com fundos escuros/pretos."
            : "Versão para FUNDO CLARO: use cores escuras e saturadas para o texto. O texto deve ser preto ou em cor escura que contraste com fundos claros/brancos.";

        return $"""
            Crie um logotipo profissional VERTICAL (proporção 2:3, retrato) para a marca "{brandName}".

            REQUISITOS OBRIGATÓRIOS:
            - Formato vertical/retrato, proporção 2:3
            - Fundo TRANSPARENTE (sem fundo)
            - Ícone/símbolo ACIMA e nome "{brandName}" ABAIXO
            - Layout vertical empilhado
            - Design limpo, vetorial, profissional
            - O nome deve ser claramente legível
            - Tipografia elegante e moderna
            - Sem mockups, sem sombras no fundo, sem bordas decorativas

            {colorScheme}

            {basePrompt}
            """;
    }

    public static List<string> GetAvailableStyles(string promptsDir)
    {
        var styles = new List<string> { "wordmark", "lettermark", "brandmark", "combinado", "emblema", "mascote" };
        return styles.Where(s => File.Exists(Path.Combine(promptsDir, $"{s}.md"))).ToList();
    }

    public static List<string> GetAvailableRules(string promptsDir)
    {
        var rules = new List<string>
        {
            "regras-grid-circular",
            "regras-minimalismo",
            "regras-proporcao-aurea",
            "regras-grid-modular",
            "regras-flat-design",
            "regras-logo-responsivo"
        };
        return rules.Where(r => File.Exists(Path.Combine(promptsDir, $"{r}.md"))).ToList();
    }
}
