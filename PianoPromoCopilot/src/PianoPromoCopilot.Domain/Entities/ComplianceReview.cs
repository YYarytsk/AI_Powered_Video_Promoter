namespace PianoPromoCopilot.Domain.Entities;

public class ComplianceReview
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public int? SourceId { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public string? Issues { get; set; }
    public bool Approved { get; set; }
    public DateTime CreatedAt { get; set; }
}
