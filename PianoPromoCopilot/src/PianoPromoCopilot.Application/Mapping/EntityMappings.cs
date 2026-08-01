using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Domain.Entities;

namespace PianoPromoCopilot.Application.Mapping;

/// <summary>
/// Single source of truth for entity to DTO shape. These shapes are part of the public API
/// contract the Angular client depends on, so they must be defined in exactly one place -
/// previously every controller carried its own copy and SuggestionsController carried three.
///
/// These are plain methods, not expression trees. EF Core client-evaluates the final projection
/// of a query, so `.Select(x => x.ToDto())` materializes the entity and maps in memory, which is
/// what the controllers already did and keeps behaviour identical.
/// </summary>
public static class EntityMappings
{
    public static VideoDto ToDto(this YouTubeVideo v) => new()
    {
        Id = v.Id,
        YouTubeVideoId = v.YouTubeVideoId,
        Title = v.Title,
        Description = v.Description,
        PublishedAt = v.PublishedAt,
        ThumbnailUrl = v.ThumbnailUrl,
        DurationIso8601 = v.DurationIso8601,
        PrivacyStatus = v.PrivacyStatus,
        ViewCount = v.ViewCount,
        LikeCount = v.LikeCount,
        CommentCount = v.CommentCount,
        CompositionName = v.CompositionName,
        Mood = v.Mood,
        Style = v.Style,
        TargetAudience = v.TargetAudience,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };

    public static SuggestionDto ToDto(this VideoOptimizationSuggestion s) => new()
    {
        Id = s.Id,
        YouTubeVideoId = s.YouTubeVideoId,
        SuggestionType = s.SuggestionType,
        Platform = s.Platform,
        SuggestionText = s.SuggestionText,
        Score = s.Score,
        IsApproved = s.IsApproved,
        IsRejected = s.IsRejected,
        CreatedAt = s.CreatedAt
    };

    public static PromotionDraftDto ToDto(this PromotionDraft d) => new()
    {
        Id = d.Id,
        YouTubeVideoId = d.YouTubeVideoId,
        Platform = d.Platform,
        DraftText = d.DraftText,
        Status = d.Status,
        ScheduledFor = d.ScheduledFor,
        PostedAt = d.PostedAt,
        CreatedAt = d.CreatedAt
    };

    public static AnalyticsSnapshotDto ToDto(this VideoAnalyticsSnapshot s) => new()
    {
        Id = s.Id,
        YouTubeVideoId = s.YouTubeVideoId,
        SnapshotDate = s.SnapshotDate,
        Views = s.Views,
        Likes = s.Likes,
        Comments = s.Comments,
        SubscribersGained = s.SubscribersGained,
        EstimatedMinutesWatched = s.EstimatedMinutesWatched,
        AverageViewDurationSeconds = s.AverageViewDurationSeconds,
        AverageViewPercentage = s.AverageViewPercentage,
        Impressions = s.Impressions,
        ImpressionClickThroughRate = s.ImpressionClickThroughRate,
        TrafficSource = s.TrafficSource,
        Country = s.Country,
        CreatedAt = s.CreatedAt
    };
}
