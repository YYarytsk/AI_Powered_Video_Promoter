namespace PianoPromoCopilot.Application.DTOs;

public class VideoDto
{
    public int Id { get; set; }
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
}

public class CreateVideoRequest
{
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? CompositionName { get; set; }
    public string? Mood { get; set; }
    public string? Style { get; set; }
    public string? TargetAudience { get; set; }
}

public class UpdateVideoRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? CompositionName { get; set; }
    public string? Mood { get; set; }
    public string? Style { get; set; }
    public string? TargetAudience { get; set; }
}
