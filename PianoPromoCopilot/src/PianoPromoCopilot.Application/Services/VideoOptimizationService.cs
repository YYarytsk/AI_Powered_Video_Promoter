using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Domain.Entities;
using PianoPromoCopilot.Domain.Enums;

namespace PianoPromoCopilot.Application.Services;

public class VideoOptimizationService : IVideoOptimizationService
{
    private readonly ILlmService _llmService;
    private readonly IComplianceReviewService _complianceService;
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<VideoOptimizationService> _logger;

    // Structured suggestion payloads (e.g. Shorts ideas) are stored as JSON in SuggestionText.
    // Use camelCase so the stored shape matches the casing the API returns everywhere else.
    private static readonly JsonSerializerOptions StoredJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string SystemPrompt = """
        You are a YouTube growth strategist for an independent pianist who publishes original piano compositions.

        Your job is to create accurate, artistic, compliant metadata and promotion material.

        You must not:
        - suggest buying views, likes, comments, or subscribers
        - suggest fake engagement
        - suggest automated comments
        - suggest misleading thumbnails
        - suggest mass spam posting
        - make claims not supported by the video metadata
        - use celebrity names unless explicitly provided by the user
        - create clickbait that misrepresents the video

        Return valid JSON only.
        No markdown.
        No explanation outside JSON.

        The JSON schema must be:
        {
          "titles": [],
          "descriptions": [],
          "tags": [],
          "hashtags": [],
          "thumbnailIdeas": [],
          "shortsIdeas": [
            {
              "title": "",
              "hook": "",
              "suggestedTimestamp": "",
              "description": "",
              "caption": ""
            }
          ],
          "socialPosts": {
            "instagram": "",
            "tiktok": "",
            "facebook": "",
            "reddit": "",
            "x": "",
            "linkedin": "",
            "emailNewsletter": ""
          }
        }
        """;

    public VideoOptimizationService(
        ILlmService llmService,
        IComplianceReviewService complianceService,
        IAppDbContext dbContext,
        ILogger<VideoOptimizationService> logger)
    {
        _llmService = llmService;
        _complianceService = complianceService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OptimizeVideoResponse> OptimizeAsync(
        OptimizeVideoRequest request,
        CancellationToken cancellationToken = default)
    {
        var userPrompt = BuildUserPrompt(request);

        string rawJson;
        try
        {
            rawJson = await _llmService.GenerateAsync(SystemPrompt, userPrompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM generation failed, using fallback mock");
            rawJson = GetFallbackJson(request);
        }

        var response = ParseLlmResponse(rawJson, request);

        // Run compliance on all generated text - every field we persist or show must be checked
        var allTexts = new List<string>();
        allTexts.AddRange(response.Titles);
        allTexts.AddRange(response.Descriptions);
        allTexts.AddRange(response.Tags);
        allTexts.AddRange(response.Hashtags);
        allTexts.AddRange(response.ThumbnailIdeas);

        foreach (var shorts in response.ShortsIdeas)
        {
            allTexts.Add(shorts.Title);
            allTexts.Add(shorts.Hook);
            allTexts.Add(shorts.SuggestedTimestamp);
            allTexts.Add(shorts.Description);
            allTexts.Add(shorts.Caption);
        }

        allTexts.Add(response.SocialPosts.Instagram);
        allTexts.Add(response.SocialPosts.TikTok);
        allTexts.Add(response.SocialPosts.Facebook);
        allTexts.Add(response.SocialPosts.Reddit);
        allTexts.Add(response.SocialPosts.X);
        allTexts.Add(response.SocialPosts.LinkedIn);
        allTexts.Add(response.SocialPosts.EmailNewsletter);

        response.Compliance = _complianceService.ReviewAll(allTexts.Where(t => !string.IsNullOrEmpty(t)));

        // Record the verdict before acting on it, so refusals are auditable after the fact.
        await RecordComplianceReviewAsync(response.Compliance, cancellationToken);

        // Blocked content is never usable, so it must not be persisted or become approvable.
        // The response still carries the verdict so the UI can explain why nothing was saved.
        if (response.Compliance.RiskLevel == RiskLevel.Blocked)
        {
            _logger.LogWarning(
                "Compliance blocked generated content for video {VideoId} - suggestions were not saved. Issues: {Issues}",
                string.IsNullOrEmpty(request.YouTubeVideoId) ? "(none)" : request.YouTubeVideoId,
                string.Join("; ", response.Compliance.Issues));

            return response;
        }

        // Save suggestions to database if we have a video ID
        if (!string.IsNullOrEmpty(request.YouTubeVideoId))
        {
            await SaveSuggestionsAsync(request.YouTubeVideoId, response, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Writes an audit row for a compliance verdict. Approved reflects whether the content was
    /// allowed to proceed, not whether a human signed off on it - that stays on the suggestion.
    /// Auditing must never break the request it is recording, so failures are logged and swallowed.
    /// </summary>
    private async Task RecordComplianceReviewAsync(
        ComplianceSummaryDto compliance,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.ComplianceReviews.AddAsync(new ComplianceReview
            {
                SourceType = ComplianceSourceType.LlmSuggestion,
                RiskLevel = compliance.RiskLevel,
                Issues = compliance.Issues.Length > 0 ? string.Join("; ", compliance.Issues) : null,
                Approved = compliance.RiskLevel != RiskLevel.Blocked,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record compliance review audit entry");
        }
    }

    private static string BuildUserPrompt(OptimizeVideoRequest request)
    {
        return $"""
            Generate YouTube optimization metadata for this piano video:

            Title: {request.Title}
            Description: {request.Description ?? "(not provided)"}
            Composition Name: {request.CompositionName ?? "(not provided)"}
            Mood: {request.Mood ?? "(not provided)"}
            Style: {request.Style ?? "(not provided)"}
            Tempo: {request.Tempo ?? "(not provided)"}
            Key Signature: {request.KeySignature ?? "(not provided)"}
            Target Audience: {request.TargetAudience ?? "(not provided)"}
            Story Behind Composition: {request.StoryBehindComposition ?? "(not provided)"}
            Current Tags: {(request.CurrentTags?.Length > 0 ? string.Join(", ", request.CurrentTags) : "(none)")}
            Video URL: {request.VideoUrl ?? "(not provided)"}

            Generate:
            - 5 alternative title options (engaging, accurate, discoverable)
            - 3 description options (include the composition story, mood, relevant keywords)
            - 20 relevant tags (mix of broad and specific piano/music terms)
            - 10 hashtags (for YouTube and social media)
            - 3 thumbnail ideas (visual concepts that convey the music's mood)
            - 2 Shorts ideas (compelling 60-second clips from the composition)
            - Social posts for all platforms (Instagram, TikTok, Facebook, Reddit, X, LinkedIn, Email Newsletter)

            All content must be authentic, accurate to the video, and compliant with YouTube Terms of Service.
            """;
    }

    private OptimizeVideoResponse ParseLlmResponse(string rawJson, OptimizeVideoRequest request)
    {
        return ParseLlmResponse(rawJson, request, isFallbackAttempt: false);
    }

    // isFallbackAttempt marks the single retry with GetFallbackJson. It makes recursion
    // impossible: if the fallback itself fails to parse we return a minimal hard-coded
    // response instead of calling back into the fallback path again.
    private OptimizeVideoResponse ParseLlmResponse(string rawJson, OptimizeVideoRequest request, bool isFallbackAttempt)
    {
        try
        {
            // Clean potential markdown code fences
            var json = rawJson.Trim();
            if (json.StartsWith("```"))
            {
                var start = json.IndexOf('\n') + 1;
                var end = json.LastIndexOf("```");

                // Only strip when the fence is actually terminated after the opening line.
                // An unterminated fence would give end < start and throw out of the JsonException
                // catch below, so leave the text as-is and let the normal parse + fallback handle it.
                if (start > 0 && end >= start)
                {
                    json = json[start..end].Trim();
                }
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var parsed = JsonSerializer.Deserialize<LlmOptimizationResult>(json, options);
            if (parsed == null)
            {
                _logger.LogWarning("LLM returned null after parse, using fallback");
                return UseFallback(request, isFallbackAttempt);
            }

            return new OptimizeVideoResponse
            {
                Titles = parsed.Titles ?? Array.Empty<string>(),
                Descriptions = parsed.Descriptions ?? Array.Empty<string>(),
                Tags = parsed.Tags ?? Array.Empty<string>(),
                Hashtags = parsed.Hashtags ?? Array.Empty<string>(),
                ThumbnailIdeas = parsed.ThumbnailIdeas ?? Array.Empty<string>(),
                ShortsIdeas = parsed.ShortsIdeas?.Select(s => new ShortsIdeaDto
                {
                    Title = s.Title ?? "",
                    Hook = s.Hook ?? "",
                    SuggestedTimestamp = s.SuggestedTimestamp ?? "",
                    Description = s.Description ?? "",
                    Caption = s.Caption ?? ""
                }).ToArray() ?? Array.Empty<ShortsIdeaDto>(),
                SocialPosts = new SocialPostsDto
                {
                    Instagram = parsed.SocialPosts?.Instagram ?? "",
                    TikTok = parsed.SocialPosts?.TikTok ?? "",
                    Facebook = parsed.SocialPosts?.Facebook ?? "",
                    Reddit = parsed.SocialPosts?.Reddit ?? "",
                    X = parsed.SocialPosts?.X ?? "",
                    LinkedIn = parsed.SocialPosts?.LinkedIn ?? "",
                    EmailNewsletter = parsed.SocialPosts?.EmailNewsletter ?? ""
                }
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM JSON response. Raw: {Raw}", rawJson[..Math.Min(500, rawJson.Length)]);
            return UseFallback(request, isFallbackAttempt);
        }
    }

    private OptimizeVideoResponse UseFallback(OptimizeVideoRequest request, bool isFallbackAttempt)
    {
        if (isFallbackAttempt)
        {
            _logger.LogError("Fallback JSON could not be parsed either, returning minimal response");
            return GetMinimalResponse(request);
        }

        return ParseLlmResponse(GetFallbackJson(request), request, isFallbackAttempt: true);
    }

    // Last resort when even the fallback JSON fails to parse. Built in code rather than from
    // JSON so it can never fail, and deliberately conservative and compliant.
    private static OptimizeVideoResponse GetMinimalResponse(OptimizeVideoRequest request)
    {
        var composition = string.IsNullOrWhiteSpace(request.CompositionName)
            ? request.Title
            : request.CompositionName;

        return new OptimizeVideoResponse
        {
            Titles = new[] { $"{composition} - Original Piano Composition" },
            Descriptions = new[] { $"An original piano composition titled '{composition}'." },
            Tags = new[] { "piano music", "original piano", "piano composition", "solo piano" },
            Hashtags = new[] { "#piano", "#originalmusic", "#pianocomposition" },
            ThumbnailIdeas = new[] { "Close-up shot of hands on piano keys with soft warm lighting" },
            ShortsIdeas = Array.Empty<ShortsIdeaDto>(),
            SocialPosts = new SocialPostsDto()
        };
    }

    private static string GetFallbackJson(OptimizeVideoRequest request)
    {
        var title = request.Title;
        var rawMood = string.IsNullOrWhiteSpace(request.Mood) ? "peaceful" : request.Mood;
        var rawComposition = request.CompositionName ?? title;
        var rawStyle = request.Style ?? "classical";

        // These values come from the user and are interpolated into JSON string literals, so
        // they must be JSON-escaped - a single quote or backslash in a title would otherwise
        // make the fallback itself unparseable.
        var mood = JsonEncodedText.Encode(rawMood).ToString();
        var moodCapitalized = JsonEncodedText.Encode(rawMood.First().ToString().ToUpper() + rawMood[1..]).ToString();
        var composition = JsonEncodedText.Encode(rawComposition).ToString();
        var style = JsonEncodedText.Encode(rawStyle).ToString();

        return $$"""
            {
              "titles": [
                "{{composition}} - Original Piano Composition",
                "{{moodCapitalized}} {{style}} Piano - {{composition}}",
                "{{composition}} | Original Piano Music",
                "Soothing Piano: {{composition}}",
                "{{composition}} - Piano Piece by Independent Composer"
              ],
              "descriptions": [
                "A {{mood}} original piano composition titled '{{composition}}'. This piece was crafted to evoke a sense of stillness and reflection. Perfect for studying, relaxing, or simply enjoying a moment of musical beauty.\n\nSubscribe for more original piano music: [channel link]\n\n#piano #originalmusic #pianocomposition",
                "{{composition}} is an original piano work exploring {{mood}} themes in a {{style}} style. Each phrase is designed to carry the listener through a journey of musical expression.\n\nIf you enjoy original piano music, please like and subscribe!\n\n#pianomusic #composer #classicalpiano",
                "Original piano composition: {{composition}}. Composed and performed by the channel artist. This piece represents an exploration of {{mood}} emotion through acoustic piano.\n\nTimestamps:\n0:00 - Introduction\n0:30 - Main Theme\n2:00 - Development\n3:30 - Recapitulation"
              ],
              "tags": [
                "piano music", "original piano", "piano composition", "{{mood}} piano", "{{style}} piano",
                "original music", "piano performance", "acoustic piano", "solo piano", "piano pieces",
                "relaxing piano", "beautiful piano music", "piano composer", "independent musician",
                "piano instrumental", "study music piano", "calm piano music", "piano melody",
                "original composition", "piano recital"
              ],
              "hashtags": [
                "#piano", "#originalmusic", "#pianocomposition", "#classicalpiano", "#pianomusic",
                "#pianist", "#composer", "#acousticpiano", "#relaxingmusic", "#originalcomposition"
              ],
              "thumbnailIdeas": [
                "Close-up shot of hands on piano keys with soft warm lighting, conveying intimacy and craftsmanship",
                "Wide shot of pianist at grand piano with dramatic natural light, creating an atmospheric mood",
                "Abstract focus on piano strings inside the instrument with text overlay of the composition title"
              ],
              "shortsIdeas": [
                {
                  "title": "The Most Emotional Moment in {{composition}}",
                  "hook": "This 10 seconds will give you chills...",
                  "suggestedTimestamp": "2:15",
                  "description": "Highlighting the climactic phrase of {{composition}} - an original piano composition",
                  "caption": "Original piano composition - {{composition}} 🎹 #piano #pianomusic #originalmusic"
                },
                {
                  "title": "How I Composed {{composition}}",
                  "hook": "Every great piece starts with a single idea...",
                  "suggestedTimestamp": "0:00",
                  "description": "Behind the scenes of composing {{composition}}, showing the creative process",
                  "caption": "Behind the composition 🎹 #pianocomposer #originalmusic #piano"
                }
              ],
              "socialPosts": {
                "instagram": "🎹 New original piano composition just dropped!\n\n'{{composition}}' is a {{mood}} piece that I've been crafting for weeks. Every note tells a story.\n\nLink in bio to watch the full performance 🎵\n\n#piano #originalmusic #pianocomposition #pianist #classicalpiano #composer #acousticpiano #pianomusic",
                "tiktok": "POV: you discover a {{mood}} original piano piece at midnight 🌙🎹\n\n'{{composition}}' - link in bio!\n\n#piano #pianomusic #originalcomposition #classicalpiano #composer",
                "facebook": "I'm thrilled to share my latest original piano composition: '{{composition}}'\n\nThis {{mood}} piece represents hours of crafting, refining, and pouring emotion into every phrase. I hope it brings you a moment of peace today.\n\nWatch the full performance here: [video link]\n\nLet me know in the comments what you feel when you hear it! 🎹",
                "reddit": "I just released an original piano composition called '{{composition}}' - it's a {{mood}} {{style}} piece. I've been composing and playing piano independently and would love any feedback from this community. Here's the link: [video link]",
                "x": "New original piano composition: '{{composition}}' 🎹\n\nA {{mood}} piece I've been working on. Full performance on YouTube - link below.\n\n#piano #originalmusic #composer",
                "linkedin": "I recently released an original piano composition titled '{{composition}}' on my YouTube channel. As an independent composer and pianist, creating authentic music that connects with listeners is my primary goal. This {{mood}} {{style}} piece represents my artistic vision for accessible, emotional piano music. I'd be honored if you'd take a few minutes to listen: [video link]",
                "emailNewsletter": "Subject: New Original Piano Composition: {{composition}}\n\nHello,\n\nI'm excited to share my latest original piano composition with you.\n\n**{{composition}}**\n\nThis {{mood}} piece was composed to evoke a sense of {{mood}} reflection. I hope it brings you a moment of beauty in your day.\n\n[Watch on YouTube →]\n\nAs always, your support means the world to me. If you enjoy this piece, sharing it with a friend who loves piano music would be the greatest gift.\n\nWith gratitude,\n[Your Name]"
              }
            }
            """;
    }

    private async Task SaveSuggestionsAsync(
        string youtubeVideoId,
        OptimizeVideoResponse response,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var suggestions = new List<VideoOptimizationSuggestion>();

        foreach (var title in response.Titles)
        {
            suggestions.Add(new VideoOptimizationSuggestion
            {
                YouTubeVideoId = youtubeVideoId,
                SuggestionType = SuggestionType.Title,
                SuggestionText = title,
                Platform = "YouTube",
                CreatedAt = now
            });
        }

        foreach (var desc in response.Descriptions)
        {
            suggestions.Add(new VideoOptimizationSuggestion
            {
                YouTubeVideoId = youtubeVideoId,
                SuggestionType = SuggestionType.Description,
                SuggestionText = desc,
                Platform = "YouTube",
                CreatedAt = now
            });
        }

        if (response.Tags.Length > 0)
        {
            suggestions.Add(new VideoOptimizationSuggestion
            {
                YouTubeVideoId = youtubeVideoId,
                SuggestionType = SuggestionType.Tags,
                SuggestionText = string.Join(", ", response.Tags),
                Platform = "YouTube",
                CreatedAt = now
            });
        }

        if (response.Hashtags.Length > 0)
        {
            suggestions.Add(new VideoOptimizationSuggestion
            {
                YouTubeVideoId = youtubeVideoId,
                SuggestionType = SuggestionType.Hashtags,
                SuggestionText = string.Join(" ", response.Hashtags),
                Platform = "YouTube",
                CreatedAt = now
            });
        }

        foreach (var idea in response.ThumbnailIdeas)
        {
            suggestions.Add(new VideoOptimizationSuggestion
            {
                YouTubeVideoId = youtubeVideoId,
                SuggestionType = SuggestionType.ThumbnailIdea,
                SuggestionText = idea,
                Platform = "YouTube",
                CreatedAt = now
            });
        }

        foreach (var shorts in response.ShortsIdeas)
        {
            suggestions.Add(new VideoOptimizationSuggestion
            {
                YouTubeVideoId = youtubeVideoId,
                SuggestionType = SuggestionType.ShortsIdea,
                SuggestionText = JsonSerializer.Serialize(shorts, StoredJsonOptions),
                Platform = "YouTube",
                CreatedAt = now
            });
        }

        var socialPosts = new[]
        {
            ("Instagram", response.SocialPosts.Instagram),
            ("TikTok", response.SocialPosts.TikTok),
            ("Facebook", response.SocialPosts.Facebook),
            ("Reddit", response.SocialPosts.Reddit),
            ("X", response.SocialPosts.X),
            ("LinkedIn", response.SocialPosts.LinkedIn),
            ("Email", response.SocialPosts.EmailNewsletter),
        };

        foreach (var (platform, text) in socialPosts)
        {
            if (!string.IsNullOrEmpty(text))
            {
                suggestions.Add(new VideoOptimizationSuggestion
                {
                    YouTubeVideoId = youtubeVideoId,
                    SuggestionType = SuggestionType.SocialPost,
                    SuggestionText = text,
                    Platform = platform,
                    CreatedAt = now
                });
            }
        }

        // Re-optimizing (and generating drafts, which optimizes internally) must not append a
        // second copy of content that is already stored. Existing rows are left untouched so the
        // user's approve/reject decisions survive.
        var existing = await _dbContext.VideoOptimizationSuggestions
            .Where(s => s.YouTubeVideoId == youtubeVideoId)
            .Select(s => new { s.SuggestionType, s.Platform, s.SuggestionText })
            .ToListAsync(cancellationToken);

        var seen = new HashSet<(string SuggestionType, string? Platform, string SuggestionText)>(
            existing.Select(s => (s.SuggestionType, s.Platform, s.SuggestionText)));

        // HashSet.Add returns false for anything already stored, and also collapses duplicates
        // within this batch.
        var newSuggestions = suggestions
            .Where(s => seen.Add((s.SuggestionType, s.Platform, s.SuggestionText)))
            .ToList();

        if (newSuggestions.Count > 0)
        {
            await _dbContext.VideoOptimizationSuggestions.AddRangeAsync(newSuggestions, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Saved {Count} new suggestions for video {VideoId} ({Skipped} duplicates skipped)",
            newSuggestions.Count, youtubeVideoId, suggestions.Count - newSuggestions.Count);
    }

    // Internal DTO for JSON deserialization from LLM
    private class LlmOptimizationResult
    {
        public string[]? Titles { get; set; }
        public string[]? Descriptions { get; set; }
        public string[]? Tags { get; set; }
        public string[]? Hashtags { get; set; }
        public string[]? ThumbnailIdeas { get; set; }
        public LlmShortsIdea[]? ShortsIdeas { get; set; }
        public LlmSocialPosts? SocialPosts { get; set; }
    }

    private class LlmShortsIdea
    {
        public string? Title { get; set; }
        public string? Hook { get; set; }
        public string? SuggestedTimestamp { get; set; }
        public string? Description { get; set; }
        public string? Caption { get; set; }
    }

    private class LlmSocialPosts
    {
        public string? Instagram { get; set; }
        public string? TikTok { get; set; }
        public string? Facebook { get; set; }
        public string? Reddit { get; set; }
        public string? X { get; set; }
        public string? LinkedIn { get; set; }
        public string? EmailNewsletter { get; set; }
    }
}
