namespace PianoPromoCopilot.Application.DTOs;

public class PromotionDraftDto
{
    public int Id { get; set; }
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string DraftText { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime? ScheduledFor { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GeneratePromotionDraftsRequest
{
    public string? AdditionalContext { get; set; }
}

public class UpdatePromotionDraftRequest
{
    public string? DraftText { get; set; }
    public string? Status { get; set; }
    public DateTime? ScheduledFor { get; set; }
}
