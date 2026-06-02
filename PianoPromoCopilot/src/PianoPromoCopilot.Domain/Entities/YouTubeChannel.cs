namespace PianoPromoCopilot.Domain.Entities;

public class YouTubeChannel
{
    public int Id { get; set; }
    public int? AppUserId { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AccessTokenEncrypted { get; set; }
    public string? RefreshTokenEncrypted { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public bool IsConnected { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public AppUser? AppUser { get; set; }
    public ICollection<YouTubeVideo> Videos { get; set; } = new List<YouTubeVideo>();
}
