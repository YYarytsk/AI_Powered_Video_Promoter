using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Domain.Entities;

namespace PianoPromoCopilot.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<YouTubeChannel> YouTubeChannels => Set<YouTubeChannel>();
    public DbSet<YouTubeVideo> YouTubeVideos => Set<YouTubeVideo>();
    public DbSet<VideoOptimizationSuggestion> VideoOptimizationSuggestions => Set<VideoOptimizationSuggestion>();
    public DbSet<PromotionDraft> PromotionDrafts => Set<PromotionDraft>();
    public DbSet<VideoAnalyticsSnapshot> VideoAnalyticsSnapshots => Set<VideoAnalyticsSnapshot>();
    public DbSet<ComplianceReview> ComplianceReviews => Set<ComplianceReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(250).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
        });

        // YouTubeChannel
        modelBuilder.Entity<YouTubeChannel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ChannelId).HasMaxLength(100).IsRequired();
            e.Property(x => x.ChannelTitle).HasMaxLength(250).IsRequired();
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1000);
            e.Property(x => x.IsConnected).HasDefaultValue(false);
            e.HasOne(x => x.AppUser)
                .WithMany(x => x.YouTubeChannels)
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // YouTubeVideo
        modelBuilder.Entity<YouTubeVideo>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.YouTubeVideoId);
            e.Property(x => x.YouTubeVideoId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1000);
            e.Property(x => x.DurationIso8601).HasMaxLength(100);
            e.Property(x => x.PrivacyStatus).HasMaxLength(50);
            e.Property(x => x.CompositionName).HasMaxLength(250);
            e.Property(x => x.Mood).HasMaxLength(250);
            e.Property(x => x.Style).HasMaxLength(250);
            e.Property(x => x.TargetAudience).HasMaxLength(500);
            e.HasOne(x => x.YouTubeChannel)
                .WithMany(x => x.Videos)
                .HasForeignKey(x => x.YouTubeChannelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // VideoOptimizationSuggestion
        modelBuilder.Entity<VideoOptimizationSuggestion>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.YouTubeVideoId);
            e.Property(x => x.YouTubeVideoId).HasMaxLength(50).IsRequired();
            e.Property(x => x.SuggestionType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Platform).HasMaxLength(100);
            e.Property(x => x.Score).HasPrecision(5, 2);
            e.Property(x => x.IsApproved).HasDefaultValue(false);
            e.Property(x => x.IsRejected).HasDefaultValue(false);
            // Relationship via foreign key string
            e.HasOne(x => x.YouTubeVideo)
                .WithMany(x => x.Suggestions)
                .HasForeignKey(x => x.YouTubeVideoId)
                .HasPrincipalKey(x => x.YouTubeVideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PromotionDraft
        modelBuilder.Entity<PromotionDraft>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.YouTubeVideoId);
            e.Property(x => x.YouTubeVideoId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Platform).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Draft");
            e.HasOne(x => x.YouTubeVideo)
                .WithMany(x => x.PromotionDrafts)
                .HasForeignKey(x => x.YouTubeVideoId)
                .HasPrincipalKey(x => x.YouTubeVideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VideoAnalyticsSnapshot
        modelBuilder.Entity<VideoAnalyticsSnapshot>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.YouTubeVideoId);
            e.Property(x => x.YouTubeVideoId).HasMaxLength(50).IsRequired();
            e.Property(x => x.EstimatedMinutesWatched).HasPrecision(18, 2);
            e.Property(x => x.AverageViewPercentage).HasPrecision(10, 4);
            e.Property(x => x.ImpressionClickThroughRate).HasPrecision(10, 4);
            e.Property(x => x.TrafficSource).HasMaxLength(250);
            e.Property(x => x.Country).HasMaxLength(20);
            e.HasOne(x => x.YouTubeVideo)
                .WithMany(x => x.AnalyticsSnapshots)
                .HasForeignKey(x => x.YouTubeVideoId)
                .HasPrincipalKey(x => x.YouTubeVideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ComplianceReview
        modelBuilder.Entity<ComplianceReview>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceType).HasMaxLength(100).IsRequired();
            e.Property(x => x.RiskLevel).HasMaxLength(50).IsRequired();
            e.Property(x => x.Approved).HasDefaultValue(false);
        });
    }
}
