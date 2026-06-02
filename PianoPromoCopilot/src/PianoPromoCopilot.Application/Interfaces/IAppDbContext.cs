using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Domain.Entities;

namespace PianoPromoCopilot.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<AppUser> AppUsers { get; }
    DbSet<YouTubeChannel> YouTubeChannels { get; }
    DbSet<YouTubeVideo> YouTubeVideos { get; }
    DbSet<VideoOptimizationSuggestion> VideoOptimizationSuggestions { get; }
    DbSet<PromotionDraft> PromotionDrafts { get; }
    DbSet<VideoAnalyticsSnapshot> VideoAnalyticsSnapshots { get; }
    DbSet<ComplianceReview> ComplianceReviews { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
