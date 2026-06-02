namespace PianoPromoCopilot.Application.DTOs;

public class YouTubeChannelDto
{
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsConnected { get; set; }
}

public class YouTubeVideoDto
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? DurationIso8601 { get; set; }
    public string? PrivacyStatus { get; set; }
    public long? ViewCount { get; set; }
    public long? LikeCount { get; set; }
    public long? CommentCount { get; set; }
}

public class UpdateYouTubeVideoMetadataRequest
{
    public string VideoId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string[]? Tags { get; set; }
}
