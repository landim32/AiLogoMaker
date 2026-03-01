namespace AiLogoMaker.Domain.Models;

public enum SessionStep
{
    BaseLogo,
    IconLogo,
    FormatVariants,
    DarkVariants,
    Export,
    Completed
}

public enum ImageApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public class ImageHistoryEntry
{
    public required string Prompt { get; set; }
    public required DateTime Timestamp { get; set; }
    public required string Action { get; set; }
    public string? AdjustmentFeedback { get; set; }
}

public class SessionImage
{
    public required string ImageId { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required LogoVariant Variant { get; set; }
    public required ImageApprovalStatus Status { get; set; }
    public required string CurrentPrompt { get; set; }
    public required List<ImageHistoryEntry> History { get; set; }
}

public class Session
{
    public required string SessionId { get; set; }
    public required string BrandName { get; set; }
    public required string Description { get; set; }
    public required string OutputDirectory { get; set; }
    public required string SelectedStyle { get; set; }
    public required List<string> SelectedRules { get; set; }
    public required SessionStep CurrentStep { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required List<SessionImage> Images { get; set; }
}
