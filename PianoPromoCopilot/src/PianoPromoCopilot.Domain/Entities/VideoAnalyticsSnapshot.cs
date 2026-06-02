namespace PianoPromoCopilot.Domain.Entities;

public class VideoAnalyticsSnapshot
{
    public int Id { get; set; }
    public string YouTubeVideoId { get; set; } = string.Empty;
    public DateOnly SnapshotDate { get; set; }
    public long? Views { get; set; }
    public long? Likes { get; set; }
    public long? Comments { get; set; }
    public long? SubscribersGained { get; set; }
    public decimal? EstimatedMinutesWatched { get; set; }
    public int? AverageViewDurationSeconds { get; set; }
    public decimal? AverageViewPercentage { get; set; }
    public long? Impressions { get; set; }
    public decimal? ImpressionClickThroughRate { get; set; }
    public string? TrafficSource { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; }

    public YouTubeVideo? YouTubeVideo { get; set; }
}
