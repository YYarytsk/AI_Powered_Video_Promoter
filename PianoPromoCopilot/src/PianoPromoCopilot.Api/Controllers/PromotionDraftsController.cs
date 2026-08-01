using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Application.Mapping;
using PianoPromoCopilot.Domain.Enums;

namespace PianoPromoCopilot.Api.Controllers;

[ApiController]
public class PromotionDraftsController : ControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly IPromotionDraftService _promotionDraftService;

    public PromotionDraftsController(
        IAppDbContext dbContext,
        IPromotionDraftService promotionDraftService)
    {
        _dbContext = dbContext;
        _promotionDraftService = promotionDraftService;
    }

    /// <summary>Returns all promotion drafts for a video.</summary>
    [HttpGet("api/videos/{youtubeVideoId}/promotion-drafts")]
    [ProducesResponseType(typeof(IEnumerable<PromotionDraftDto>), 200)]
    public async Task<IActionResult> GetDrafts(string youtubeVideoId, CancellationToken cancellationToken)
    {
        var drafts = await _dbContext.PromotionDrafts
            .Where(d => d.YouTubeVideoId == youtubeVideoId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => d.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(drafts);
    }

    /// <summary>Generates promotion drafts for all platforms using LLM and saves them.</summary>
    [HttpPost("api/videos/{youtubeVideoId}/promotion-drafts")]
    [ProducesResponseType(typeof(IEnumerable<PromotionDraftDto>), 201)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> GenerateDrafts(
        string youtubeVideoId,
        [FromBody] GeneratePromotionDraftsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _promotionDraftService.GenerateDraftsAsync(youtubeVideoId, cancellationToken);

        if (result.Outcome == GeneratePromotionDraftsOutcome.VideoNotFound)
        {
            return NotFound(new { message = $"Video '{youtubeVideoId}' not found" });
        }

        // Surface the compliance verdict to the client without changing the response body shape
        var compliance = result.Compliance;
        Response.Headers["X-Compliance-Risk"] = compliance.RiskLevel;
        Response.Headers["X-Compliance-Safe"] = compliance.IsSafeToUse ? "true" : "false";
        Response.Headers["X-Compliance-Issue-Count"] = compliance.Issues.Length.ToString();

        if (result.Outcome == GeneratePromotionDraftsOutcome.ComplianceBlocked)
        {
            return UnprocessableEntity(new
            {
                message = "Compliance review failed. No promotion drafts were saved.",
                riskLevel = compliance.RiskLevel,
                issues = compliance.Issues
            });
        }

        return CreatedAtAction(nameof(GetDrafts), new { youtubeVideoId }, result.Drafts);
    }

    /// <summary>Updates a promotion draft's text or status.</summary>
    [HttpPut("api/promotion-drafts/{id:int}")]
    [ProducesResponseType(typeof(PromotionDraftDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateDraft(
        int id,
        [FromBody] UpdatePromotionDraftRequest request,
        CancellationToken cancellationToken)
    {
        var draft = await _dbContext.PromotionDrafts
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (draft == null)
            return NotFound(new { message = $"Promotion draft {id} not found" });

        if (request.DraftText != null) draft.DraftText = request.DraftText;
        if (request.Status != null) draft.Status = request.Status;
        if (request.ScheduledFor.HasValue) draft.ScheduledFor = request.ScheduledFor;
        if (request.Status == PromotionStatus.PostedManually) draft.PostedAt = DateTime.UtcNow;
        draft.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(draft.ToDto());
    }
}
