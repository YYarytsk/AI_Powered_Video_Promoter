using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Application.Services;
using PianoPromoCopilot.Domain.Entities;
using PianoPromoCopilot.Infrastructure.Data;
using Xunit;

namespace PianoPromoCopilot.Tests.Services;

public class YouTubeSyncServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Mock<IYouTubeService> CreateYouTube(
        YouTubeChannelDto? channel = null,
        params YouTubeVideoDto[] videos)
    {
        var mock = new Mock<IYouTubeService>();
        mock.Setup(y => y.GetChannelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel ?? new YouTubeChannelDto
            {
                ChannelId = "UCmock_001",
                ChannelTitle = "Demo Channel",
                IsConnected = false
            });
        mock.Setup(y => y.GetVideosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(videos);
        return mock;
    }

    private static YouTubeSyncService CreateService(IYouTubeService youTube, IAppDbContext db) =>
        new(youTube, db, NullLogger<YouTubeSyncService>.Instance);

    [Fact]
    public async Task SyncVideosAsync_NewVideos_AreInserted()
    {
        var db = CreateInMemoryDbContext();
        var youTube = CreateYouTube(null,
            new YouTubeVideoDto { VideoId = "v1", Title = "One", ViewCount = 10 },
            new YouTubeVideoDto { VideoId = "v2", Title = "Two", ViewCount = 20 });

        var returned = await CreateService(youTube.Object, db).SyncVideosAsync();

        returned.Should().HaveCount(2);
        db.YouTubeVideos.Should().HaveCount(2);
        db.YouTubeVideos.Single(v => v.YouTubeVideoId == "v1").Title.Should().Be("One");
    }

    [Fact]
    public async Task SyncVideosAsync_ExistingVideo_RefreshesStatsWithoutDuplicatingOrLosingCuration()
    {
        var db = CreateInMemoryDbContext();
        db.YouTubeVideos.Add(new YouTubeVideo
        {
            YouTubeVideoId = "v1",
            Title = "Old title",
            ViewCount = 1,
            CompositionName = "Moonlit Reverie",
            Mood = "peaceful",
            Style = "impressionist",
            TargetAudience = "piano listeners",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var youTube = CreateYouTube(null,
            new YouTubeVideoDto { VideoId = "v1", Title = "New title", ViewCount = 99, LikeCount = 7 });

        await CreateService(youTube.Object, db).SyncVideosAsync();

        db.YouTubeVideos.Should().HaveCount(1, "syncing must upsert, not append");

        var video = db.YouTubeVideos.Single();
        video.Title.Should().Be("New title");
        video.ViewCount.Should().Be(99);
        video.LikeCount.Should().Be(7);
        video.UpdatedAt.Should().NotBeNull();

        // Locally curated metadata is the user's and is never overwritten by a sync.
        video.CompositionName.Should().Be("Moonlit Reverie");
        video.Mood.Should().Be("peaceful");
        video.Style.Should().Be("impressionist");
        video.TargetAudience.Should().Be("piano listeners");
    }

    [Fact]
    public async Task SyncChannelAndVideosAsync_RunTwice_KeepsOneChannelRowAndRefreshesIt()
    {
        var db = CreateInMemoryDbContext();
        var youTube = new Mock<IYouTubeService>();
        youTube.SetupSequence(y => y.GetChannelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YouTubeChannelDto { ChannelId = "UC1", ChannelTitle = "First" })
            .ReturnsAsync(new YouTubeChannelDto { ChannelId = "UC1", ChannelTitle = "Renamed" });
        youTube.Setup(y => y.GetVideosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new YouTubeVideoDto { VideoId = "v1", Title = "One" } });

        var sut = CreateService(youTube.Object, db);

        await sut.SyncChannelAndVideosAsync();
        var second = await sut.SyncChannelAndVideosAsync();

        second.ChannelTitle.Should().Be("Renamed");
        second.VideosSynced.Should().Be(1);

        db.YouTubeChannels.Should().HaveCount(1);
        db.YouTubeChannels.Single().ChannelTitle.Should().Be("Renamed",
            "a channel rename must reach the local row, not be dropped because the row already existed");
        db.YouTubeVideos.Should().HaveCount(1);
    }

    [Fact]
    public async Task SyncChannelAndVideosAsync_PreservesStoredOAuthTokens()
    {
        var db = CreateInMemoryDbContext();
        db.YouTubeChannels.Add(new YouTubeChannel
        {
            ChannelId = "UC1",
            ChannelTitle = "First",
            AccessTokenEncrypted = "access",
            RefreshTokenEncrypted = "refresh",
            IsConnected = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var youTube = CreateYouTube(new YouTubeChannelDto
        {
            ChannelId = "UC1",
            ChannelTitle = "Renamed",
            IsConnected = true
        });

        await CreateService(youTube.Object, db).SyncChannelAndVideosAsync();

        var channel = db.YouTubeChannels.Single();
        channel.AccessTokenEncrypted.Should().Be("access");
        channel.RefreshTokenEncrypted.Should().Be("refresh");
    }

    [Fact]
    public async Task SyncVideosAsync_NoVideos_DoesNotThrow()
    {
        var db = CreateInMemoryDbContext();
        var youTube = CreateYouTube();

        var result = await CreateService(youTube.Object, db).SyncVideosAsync();

        result.Should().BeEmpty();
        db.YouTubeVideos.Should().BeEmpty();
    }
}
