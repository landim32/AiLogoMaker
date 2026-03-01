using System.Text.Json;
using System.Text.Json.Serialization;
using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Infra.Repositories;

public class FileSystemSessionRepository : ISessionRepository
{
    private const string SessionFileName = "session.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task SaveAsync(Session session)
    {
        session.UpdatedAt = DateTime.UtcNow;
        Directory.CreateDirectory(session.OutputDirectory);
        var filePath = Path.Combine(session.OutputDirectory, SessionFileName);
        var json = JsonSerializer.Serialize(session, s_jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<Session?> LoadAsync(string outputDirectory)
    {
        var filePath = Path.Combine(outputDirectory, SessionFileName);
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Session>(json, s_jsonOptions);
    }

    public async Task<List<Session>> FindAllSessionsAsync(string outputRootDirectory)
    {
        var sessions = new List<Session>();

        if (!Directory.Exists(outputRootDirectory))
            return sessions;

        foreach (var dir in Directory.GetDirectories(outputRootDirectory))
        {
            var session = await LoadAsync(dir);
            if (session != null)
                sessions.Add(session);
        }

        return sessions
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();
    }
}
