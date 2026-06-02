using System.ComponentModel.DataAnnotations;

namespace PianoPromoCopilot.Application.DTOs;

public class OptimizeVideoRequest
{
    public string? YouTubeVideoId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? CompositionName { get; set; }
    public string? Mood { get; set; }
    public string? Style { get; set; }
    public string? Tempo { get; set; }
    public string? KeySignature { get; set; }
    public string? TargetAudience { get; set; }
    public string? StoryBehindComposition { get; set; }
    public string[]? CurrentTags { get; set; }
    public string? VideoUrl { get; set; }
}
