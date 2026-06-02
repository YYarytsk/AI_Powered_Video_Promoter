using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;

namespace PianoPromoCopilot.Api.Controllers;

[ApiController]
[Route("api/videos/{youtubeVideoId}/suggestions")]
public class SuggestionsController : ControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<SuggestionsController> _logger;

    public SuggestionsController(IAppDbContext dbContext, ILogger<SuggestionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>Returns all saved optimization suggestions for a video.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SuggestionDto>), 200)]
    public async Task<IActionResult> GetSuggestions(string youtubeVideoId, CancellationToken cancellationToken)
    {
        var suggestions = await _dbContext.VideoOptimizationSuggestions
            .Where(s => s.YouTubeVideoId == youtubeVideoId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SuggestionDto
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
            })
            .ToListAsync(cancellationToken);

        return Ok(suggestions);
    }

    /// <summary>Marks a suggestion as approved (human-reviewed).</summary>
    [HttpPost("{suggestionId}/approve")]
    [ProducesResponseType(typeof(SuggestionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApproveSuggestion(
        string youtubeVideoId,
        int suggestionId,
        CancellationToken cancellationToken)
    {
        var suggestion = await _dbContext.VideoOptimizationSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.YouTubeVideoId == youtubeVideoId, cancellationToken);

        if (suggestion == null)
            return NotFound(new { message = $"Suggestion {suggestionId} not found for video {youtubeVideoId}" });

        suggestion.IsApproved = true;
        suggestion.IsRejected = false;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Suggestion {SuggestionId} approved for video {VideoId}", suggestionId, youtubeVideoId);

        return Ok(new SuggestionDto
        {
            Id = suggestion.Id,
            YouTubeVideoId = suggestion.YouTubeVideoId,
            SuggestionType = suggestion.SuggestionType,
            Platform = suggestion.Platform,
            SuggestionText = suggestion.SuggestionText,
            Score = suggestion.Score,
            IsApproved = suggestion.IsApproved,
            IsRejected = suggestion.IsRejected,
            CreatedAt = suggestion.CreatedAt
        });
    }

    /// <summary>Marks a suggestion as rejected.</summary>
    [HttpPost("{suggestionId}/reject")]
    [ProducesResponseType(typeof(SuggestionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RejectSuggestion(
        string youtubeVideoId,
        int suggestionId,
        CancellationToken cancellationToken)
    {
        var suggestion = await _dbContext.VideoOptimizationSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.YouTubeVideoId == youtubeVideoId, cancellationToken);

        if (suggestion == null)
            return NotFound(new { message = $"Suggestion {suggestionId} not found for video {youtubeVideoId}" });

        suggestion.IsApproved = false;
        suggestion.IsRejected = true;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Suggestion {SuggestionId} rejected for video {VideoId}", suggestionId, youtubeVideoId);

        return Ok(new SuggestionDto
        {
            Id = suggestion.Id,
            YouTubeVideoId = suggestion.YouTubeVideoId,
            SuggestionType = suggestion.SuggestionType,
            Platform = suggestion.Platform,
            SuggestionText = suggestion.SuggestionText,
            Score = suggestion.Score,
            IsApproved = suggestion.IsApproved,
            IsRejected = suggestion.IsRejected,
            CreatedAt = suggestion.CreatedAt
        });
    }
}
