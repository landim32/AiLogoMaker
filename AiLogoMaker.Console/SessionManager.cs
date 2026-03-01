using AiLogoMaker.Domain.Interfaces;
using AiLogoMaker.Domain.Models;

namespace AiLogoMaker.Console;

public class SessionManager
{
    private readonly ISessionRepository _repository;
    private Session _session = null!;

    public SessionManager(ISessionRepository repository)
    {
        _repository = repository;
    }

    public Session Current => _session;

    public async Task<List<Session>> FindExistingSessionsAsync(string outputRoot)
    {
        return await _repository.FindAllSessionsAsync(outputRoot);
    }

    public async Task CreateNewSessionAsync(
        string brandName, string description, string outputDir,
        string selectedStyle, List<string> selectedRules)
    {
        _session = new Session
        {
            SessionId = Guid.NewGuid().ToString("N"),
            BrandName = brandName,
            Description = description,
            OutputDirectory = outputDir,
            SelectedStyle = selectedStyle,
            SelectedRules = selectedRules,
            CurrentStep = SessionStep.BaseLogo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Images = new List<SessionImage>()
        };
        await _repository.SaveAsync(_session);
    }

    public void LoadSession(Session session)
    {
        _session = session;
    }

    public async Task RecordImageGeneratedAsync(
        string imageId, string fileName, string filePath,
        LogoVariant variant, string prompt, string action = "generated")
    {
        var existing = _session.Images.FirstOrDefault(i => i.ImageId == imageId);
        if (existing != null)
        {
            existing.FileName = fileName;
            existing.FilePath = filePath;
            existing.CurrentPrompt = prompt;
            existing.Status = ImageApprovalStatus.Pending;
            existing.History.Add(new ImageHistoryEntry
            {
                Prompt = prompt,
                Timestamp = DateTime.UtcNow,
                Action = action
            });
        }
        else
        {
            _session.Images.Add(new SessionImage
            {
                ImageId = imageId,
                FileName = fileName,
                FilePath = filePath,
                Variant = variant,
                Status = ImageApprovalStatus.Pending,
                CurrentPrompt = prompt,
                History = new List<ImageHistoryEntry>
                {
                    new()
                    {
                        Prompt = prompt,
                        Timestamp = DateTime.UtcNow,
                        Action = action
                    }
                }
            });
        }

        await _repository.SaveAsync(_session);
    }

    public async Task RecordAdjustmentAsync(
        string imageId, string adjustmentFeedback, string newPrompt, string newFilePath)
    {
        var image = _session.Images.First(i => i.ImageId == imageId);
        image.FilePath = newFilePath;
        image.CurrentPrompt = newPrompt;
        image.Status = ImageApprovalStatus.Pending;
        image.History.Add(new ImageHistoryEntry
        {
            Prompt = newPrompt,
            Timestamp = DateTime.UtcNow,
            Action = "adjusted",
            AdjustmentFeedback = adjustmentFeedback
        });
        await _repository.SaveAsync(_session);
    }

    public async Task SetImageStatusAsync(string imageId, ImageApprovalStatus status)
    {
        var image = _session.Images.First(i => i.ImageId == imageId);
        image.Status = status;
        await _repository.SaveAsync(_session);
    }

    public async Task AdvanceStepAsync(SessionStep nextStep)
    {
        _session.CurrentStep = nextStep;
        await _repository.SaveAsync(_session);
    }

    public List<SessionImage> GetMissingApprovedImages()
    {
        return _session.Images
            .Where(i => i.Status == ImageApprovalStatus.Approved && !File.Exists(i.FilePath))
            .ToList();
    }

    public List<SessionImage> GetApprovedImagesByIds(params string[] imageIds)
    {
        return _session.Images
            .Where(i => i.Status == ImageApprovalStatus.Approved && imageIds.Contains(i.ImageId))
            .ToList();
    }

    public SessionImage? GetImage(string imageId)
    {
        return _session.Images.FirstOrDefault(i => i.ImageId == imageId);
    }

    public static LogoResult ToLogoResult(SessionImage image)
    {
        return new LogoResult
        {
            Name = image.FileName,
            FilePath = image.FilePath,
            Variant = image.Variant,
            Prompt = image.CurrentPrompt
        };
    }

    public static List<LogoResult> ToLogoResults(IEnumerable<SessionImage> images)
    {
        return images.Select(ToLogoResult).ToList();
    }
}
