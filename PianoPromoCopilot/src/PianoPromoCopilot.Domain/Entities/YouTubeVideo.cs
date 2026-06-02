namespace PianoPromoCopilot.Domain.Entities;

public class YouTubeVideo
{
    public int Id { get; set; }
    public int? YouTubeChannelId { get; set; }
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? DurationIso8601 { get; set; }
    public string? PrivacyStatus { get; set; }
    public long? ViewCount { get; set; }
    public long? LikeCount { get; set; }
    public long? CommentCount { get; set; }
    public string? CompositionName { get; set; }
    public string? Mood { get; set; }
    public string? Style { get; set; }
    public string? TargetAudience { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public YouTubeChannel? YouTubeChannel { get; set; }
    public ICollection<VideoOptimizationSuggestion> Suggestions { get; set; } = new List<VideoOptimizationSuggestion>();
    public ICollection<PromotionDraft> PromotionDrafts { get; set; } = new List<PromotionDraft>();
    public ICollection<VideoAnalyticsSnapshot> AnalyticsSnapshots { get; set; } = new List<VideoAnalyticsSnapshot>();
}
