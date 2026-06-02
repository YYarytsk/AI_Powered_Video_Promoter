namespace PianoPromoCopilot.Domain.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<YouTubeChannel> YouTubeChannels { get; set; } = new List<YouTubeChannel>();
}
