namespace PianoPromoCopilot.Domain.Entities;

public class PromotionDraft
{
    public int Id { get; set; }
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string DraftText { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime? ScheduledFor { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public YouTubeVideo? YouTubeVideo { get; set; }
}
