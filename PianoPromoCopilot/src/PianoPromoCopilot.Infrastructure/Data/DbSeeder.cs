using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PianoPromoCopilot.Domain.Entities;

namespace PianoPromoCopilot.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.MigrateAsync();

            if (await context.YouTubeChannels.AnyAsync())
            {
                logger.LogInformation("Database already seeded, skipping");
                return;
            }

            logger.LogInformation("Seeding database with sample data...");

            var now = DateTime.UtcNow;

            // Seed channel
            var channel = new YouTubeChannel
            {
                ChannelId = "UCmock_piano_channel_001",
                ChannelTitle = "PianoPromoCopilot Demo Channel",
                Description = "A demo channel featuring original piano compositions for testing PianoPromoCopilot.",
                ThumbnailUrl = "https://placehold.co/88x88/1a1a2e/ffffff?text=Piano",
                IsConnected = false,
                CreatedAt = now
            };
            context.YouTubeChannels.Add(channel);
            await context.SaveChangesAsync();

            // Seed videos
            var videos = new[]
            {
                new YouTubeVideo
                {
                    YouTubeChannelId = channel.Id,
                    YouTubeVideoId = "mock_video_001",
                    Title = "Moonlit Reverie - Original Piano Composition",
                    Description = "A peaceful original piano composition evoking the quiet beauty of moonlit nights. This piece was composed during a period of reflection and explores themes of stillness and wonder.",
                    PublishedAt = now.AddDays(-30),
                    ThumbnailUrl = "https://placehold.co/320x180/1a1a2e/ffffff?text=Moonlit+Reverie",
                    DurationIso8601 = "PT4M32S",
                    PrivacyStatus = "public",
                    ViewCount = 1247,
                    LikeCount = 89,
                    CommentCount = 23,
                    CompositionName = "Moonlit Reverie",
                    Mood = "peaceful",
                    Style = "impressionist",
                    TargetAudience = "classical music lovers, relaxation seekers, piano enthusiasts",
                    CreatedAt = now
                },
                new YouTubeVideo
                {
                    YouTubeChannelId = channel.Id,
                    YouTubeVideoId = "mock_video_002",
                    Title = "Storm's Edge - Dramatic Piano Solo",
                    Description = "A dramatic original piano composition capturing the intensity of an approaching storm. Features dynamic contrasts and virtuosic passages.",
                    PublishedAt = now.AddDays(-14),
                    ThumbnailUrl = "https://placehold.co/320x180/1a1a2e/ffffff?text=Storm%27s+Edge",
                    DurationIso8601 = "PT5M18S",
                    PrivacyStatus = "public",
                    ViewCount = 543,
                    LikeCount = 67,
                    CommentCount = 15,
                    CompositionName = "Storm's Edge",
                    Mood = "dramatic",
                    Style = "romantic",
                    TargetAudience = "classical piano listeners, drama lovers, music students",
                    CreatedAt = now
                },
                new YouTubeVideo
                {
                    YouTubeChannelId = channel.Id,
                    YouTubeVideoId = "mock_video_003",
                    Title = "Spring Morning - A Gentle Piano Piece",
                    Description = "A light, hopeful original piano composition inspired by the renewal of spring. Gentle melodic lines and warm harmonies create an uplifting atmosphere.",
                    PublishedAt = now.AddDays(-7),
                    ThumbnailUrl = "https://placehold.co/320x180/1a1a2e/ffffff?text=Spring+Morning",
                    DurationIso8601 = "PT3M45S",
                    PrivacyStatus = "public",
                    ViewCount = 198,
                    LikeCount = 34,
                    CommentCount = 8,
                    CompositionName = "Spring Morning",
                    Mood = "uplifting",
                    Style = "neo-classical",
                    TargetAudience = "casual music listeners, study music seekers, relaxation",
                    CreatedAt = now
                }
            };

            context.YouTubeVideos.AddRange(videos);
            await context.SaveChangesAsync();

            // Seed analytics snapshots
            var analyticsSnapshots = new List<VideoAnalyticsSnapshot>();
            var random = new Random(42);

            foreach (var video in videos)
            {
                for (int i = 7; i >= 0; i--)
                {
                    var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i));
                    analyticsSnapshots.Add(new VideoAnalyticsSnapshot
                    {
                        YouTubeVideoId = video.YouTubeVideoId,
                        SnapshotDate = date,
                        Views = (long)(video.ViewCount ?? 100) / 8 + random.Next(-10, 50),
                        Likes = (long)(video.LikeCount ?? 10) / 8 + random.Next(-2, 10),
                        Comments = (long)(video.CommentCount ?? 3) / 8 + random.Next(0, 3),
                        SubscribersGained = random.Next(0, 5),
                        EstimatedMinutesWatched = (decimal)(random.Next(50, 300)),
                        AverageViewDurationSeconds = random.Next(90, 240),
                        AverageViewPercentage = (decimal)(random.Next(35, 75)) / 100m,
                        Impressions = random.Next(500, 3000),
                        ImpressionClickThroughRate = (decimal)(random.Next(2, 8)) / 100m,
                        TrafficSource = i % 3 == 0 ? "YT_SEARCH" : i % 3 == 1 ? "EXT_URL" : "YT_CHANNEL",
                        Country = i % 4 == 0 ? "GB" : "US",
                        CreatedAt = now
                    });
                }
            }

            context.VideoAnalyticsSnapshots.AddRange(analyticsSnapshots);

            // Seed a few sample suggestions for the first video
            var suggestions = new[]
            {
                new VideoOptimizationSuggestion
                {
                    YouTubeVideoId = "mock_video_001",
                    SuggestionType = "Title",
                    SuggestionText = "Moonlit Reverie - Peaceful Original Piano Composition | Relaxing Music",
                    Platform = "YouTube",
                    IsApproved = false,
                    IsRejected = false,
                    CreatedAt = now
                },
                new VideoOptimizationSuggestion
                {
                    YouTubeVideoId = "mock_video_001",
                    SuggestionType = "Tags",
                    SuggestionText = "piano music, original piano, peaceful piano, relaxing music, piano composition, solo piano, acoustic piano, classical piano",
                    Platform = "YouTube",
                    IsApproved = true,
                    IsRejected = false,
                    CreatedAt = now
                }
            };

            context.VideoOptimizationSuggestions.AddRange(suggestions);

            // Seed a sample promotion draft
            var draft = new PromotionDraft
            {
                YouTubeVideoId = "mock_video_001",
                Platform = "Instagram",
                DraftText = "🎹 New original piano composition: 'Moonlit Reverie'\n\nThis peaceful piece was created to evoke the quiet beauty of moonlit nights. Perfect for late-night studying or unwinding after a long day.\n\nWatch the full performance - link in bio!\n\n#piano #originalmusic #pianocomposition #relaxingmusic #pianist",
                Status = "Draft",
                CreatedAt = now
            };

            context.PromotionDrafts.Add(draft);
            await context.SaveChangesAsync();

            logger.LogInformation("Database seeded successfully with {VideoCount} videos", videos.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }
}
