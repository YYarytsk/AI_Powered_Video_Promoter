using PianoPromoCopilot.Application.DTOs;

namespace PianoPromoCopilot.Application.Interfaces;

public interface IPromotionDraftService
{
    /// <summary>
    /// Generates a fresh batch of promotion drafts for a video and persists them.
    /// Compliance gates persistence: a Blocked verdict returns without writing any draft.
    /// Nothing here posts anything - drafts always await a human.
    /// </summary>
    Task<GeneratePromotionDraftsResult> GenerateDraftsAsync(
        string youTubeVideoId,
        CancellationToken cancellationToken = default);
}
