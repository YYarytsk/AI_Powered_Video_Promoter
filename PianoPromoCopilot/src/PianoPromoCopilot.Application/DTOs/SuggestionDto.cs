namespace PianoPromoCopilot.Application.DTOs;

public class SuggestionDto
{
    public int Id { get; set; }
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string SuggestionType { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public string SuggestionText { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }
    public DateTime CreatedAt { get; set; }
}
