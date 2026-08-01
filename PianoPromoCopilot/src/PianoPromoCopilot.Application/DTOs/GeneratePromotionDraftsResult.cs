namespace PianoPromoCopilot.Application.DTOs;

/// <summary>
/// Why draft generation ended the way it did. This is an in-process result discriminator, not a
/// persisted value, so it is a real enum rather than the const-string convention the Domain uses
/// for values that are written to columns.
/// </summary>
public enum GeneratePromotionDraftsOutcome
{
    Created,
    VideoNotFound,
    ComplianceBlocked
}

public class GeneratePromotionDraftsResult
{
    public GeneratePromotionDraftsOutcome Outcome { get; init; }

    /// <summary>
    /// The compliance verdict for the generated content. Empty/default when the video was not
    /// found, because nothing was generated in that case.
    /// </summary>
    public ComplianceSummaryDto Compliance { get; init; } = new();

    /// <summary>Drafts that were persisted. Always empty unless <see cref="Outcome"/> is Created.</summary>
    public IReadOnlyList<PromotionDraftDto> Drafts { get; init; } = Array.Empty<PromotionDraftDto>();

    public static GeneratePromotionDraftsResult VideoNotFound() =>
        new() { Outcome = GeneratePromotionDraftsOutcome.VideoNotFound };

    public static GeneratePromotionDraftsResult Blocked(ComplianceSummaryDto compliance) =>
        new() { Outcome = GeneratePromotionDraftsOutcome.ComplianceBlocked, Compliance = compliance };

    public static GeneratePromotionDraftsResult Created(
        ComplianceSummaryDto compliance,
        IReadOnlyList<PromotionDraftDto> drafts) =>
        new() { Outcome = GeneratePromotionDraftsOutcome.Created, Compliance = compliance, Drafts = drafts };
}
