using PianoPromoCopilot.Application.DTOs;

namespace PianoPromoCopilot.Application.Interfaces;

public interface IAnalyticsRecommendationService
{
    IReadOnlyList<AnalyticsRecommendationDto> GenerateRecommendations(IReadOnlyList<AnalyticsSnapshotDto> snapshots);
}
