using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Domain.Interfaces;

public interface ISessionRepository
{
    Task SaveAsync(Session session);
    Task<Session?> LoadAsync(string outputDirectory);
    Task<List<Session>> FindAllSessionsAsync(string outputRootDirectory);
}
