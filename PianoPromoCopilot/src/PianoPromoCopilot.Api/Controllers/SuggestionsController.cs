using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Application.Mapping;

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
            .Select(s => s.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(suggestions);
    }

    /// <summary>Marks a suggestion as approved (human-reviewed).</summary>
    [HttpPost("{suggestionId}/approve")]
    [ProducesResponseType(typeof(SuggestionDto), 200)]
    [ProducesResponseType(404)]
    public Task<IActionResult> ApproveSuggestion(
        string youtubeVideoId,
        int suggestionId,
        CancellationToken cancellationToken)
        => SetReviewDecisionAsync(youtubeVideoId, suggestionId, approved: true, cancellationToken);

    /// <summary>Marks a suggestion as rejected.</summary>
    [HttpPost("{suggestionId}/reject")]
    [ProducesResponseType(typeof(SuggestionDto), 200)]
    [ProducesResponseType(404)]
    public Task<IActionResult> RejectSuggestion(
        string youtubeVideoId,
        int suggestionId,
        CancellationToken cancellationToken)
        => SetReviewDecisionAsync(youtubeVideoId, suggestionId, approved: false, cancellationToken);

    /// <summary>
    /// Approve and reject are the same operation with opposite flags. Sharing one implementation
    /// keeps them from drifting - IsApproved and IsRejected must always be set as a pair, or a
    /// suggestion can end up both approved and rejected.
    /// </summary>
    private async Task<IActionResult> SetReviewDecisionAsync(
        string youtubeVideoId,
        int suggestionId,
        bool approved,
        CancellationToken cancellationToken)
    {
        var suggestion = await _dbContext.VideoOptimizationSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.YouTubeVideoId == youtubeVideoId, cancellationToken);

        if (suggestion == null)
            return NotFound(new { message = $"Suggestion {suggestionId} not found for video {youtubeVideoId}" });

        suggestion.IsApproved = approved;
        suggestion.IsRejected = !approved;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Suggestion {SuggestionId} {Decision} for video {VideoId}",
            suggestionId, approved ? "approved" : "rejected", youtubeVideoId);

        return Ok(suggestion.ToDto());
    }
}
