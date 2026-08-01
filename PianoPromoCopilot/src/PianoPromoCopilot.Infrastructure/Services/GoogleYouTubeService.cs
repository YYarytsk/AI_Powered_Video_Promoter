using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;

namespace PianoPromoCopilot.Infrastructure.Services;

/// <summary>
/// Google YouTube Data API v3 service implementation.
/// TODO: Implement real OAuth flow and API calls.
/// Currently a skeleton - use MockYouTubeService until OAuth is configured.
///
/// Setup required:
/// 1. Create a Google Cloud project at https://console.cloud.google.com
/// 2. Enable YouTube Data API v3
/// 3. Create OAuth 2.0 credentials (Web Application type)
/// 4. Set Google__ClientId, Google__ClientSecret, Google__RedirectUri in appsettings
/// 5. Implement the OAuth token exchange flow (see TODOs below)
/// </summary>
public class GoogleYouTubeService : IYouTubeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleYouTubeService> _logger;

    private const string YouTubeApiBaseUrl = "https://www.googleapis.com/youtube/v3/";

    public GoogleYouTubeService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleYouTubeService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(YouTubeApiBaseUrl);
    }

    public Task<YouTubeChannelDto> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement Google OAuth token refresh before API calls
        // TODO: Call GET https://www.googleapis.com/youtube/v3/channels?part=snippet,statistics&mine=true
        // TODO: Map response to YouTubeChannelDto
        // TODO: Handle token expiry and refresh using refresh token from YouTubeChannel entity

        throw new NotImplementedException(
            "Google YouTube API not yet configured. Set Features__UseMockYouTube=true in appsettings.");
    }

    public Task<IReadOnlyList<YouTubeVideoDto>> GetVideosAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Get channel uploads playlist ID from channels API
        // TODO: Call GET https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&playlistId={uploadsPlaylistId}
        // TODO: For each video, call videos API to get statistics
        // TODO: Map to YouTubeVideoDto list

        throw new NotImplementedException(
            "Google YouTube API not yet configured. Set Features__UseMockYouTube=true in appsettings.");
    }

    public Task<YouTubeVideoDto?> GetVideoAsync(string videoId, CancellationToken cancellationToken = default)
    {
        // TODO: Call GET https://www.googleapis.com/youtube/v3/videos?part=snippet,statistics,contentDetails&id={videoId}
        // TODO: Map response to YouTubeVideoDto

        throw new NotImplementedException(
            "Google YouTube API not yet configured. Set Features__UseMockYouTube=true in appsettings.");
    }

    public Task UpdateVideoMetadataAsync(
        UpdateYouTubeVideoMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        // IMPORTANT: This method must NEVER be called unless Features__EnableYouTubeWriteActions=true
        // IMPORTANT: Must preserve existing description/tags if request does not include replacement values
        // IMPORTANT: Must run compliance review before updating

        // TODO: First fetch the existing video to get current description/tags
        // TODO: Only replace fields that are explicitly provided in the request
        // TODO: Call PUT https://www.googleapis.com/youtube/v3/videos?part=snippet with updated metadata
        // TODO: Handle OAuth token refresh if needed
        // TODO: Log the exact changes being made for audit trail

        throw new NotImplementedException(
            "Google YouTube API write actions not yet configured. Set Features__EnableYouTubeWriteActions=true only after full OAuth setup.");
    }

    // TODO: Implement private method RefreshAccessTokenAsync()
    // TODO: Use IHttpClientFactory for resilient HTTP calls
    // TODO: Implement token storage back to YouTubeChannel entity via IAppDbContext
}
