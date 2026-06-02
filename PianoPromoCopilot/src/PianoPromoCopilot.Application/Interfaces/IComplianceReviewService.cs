using PianoPromoCopilot.Application.DTOs;

namespace PianoPromoCopilot.Application.Interfaces;

public interface IComplianceReviewService
{
    ComplianceSummaryDto Review(string text);
    ComplianceSummaryDto ReviewAll(IEnumerable<string> texts);
}
