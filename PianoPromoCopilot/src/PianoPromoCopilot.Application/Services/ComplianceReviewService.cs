using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Domain.Enums;

namespace PianoPromoCopilot.Application.Services;

public class ComplianceReviewService : IComplianceReviewService
{
    private static readonly (string Pattern, string RiskLevel, string Issue)[] Rules = new[]
    {
        ("buy views", RiskLevel.Blocked, "Suggests purchasing views which violates YouTube ToS"),
        ("buy likes", RiskLevel.Blocked, "Suggests purchasing likes which violates YouTube ToS"),
        ("buy subscribers", RiskLevel.Blocked, "Suggests purchasing subscribers which violates YouTube ToS"),
        ("buy comments", RiskLevel.Blocked, "Suggests purchasing comments which violates YouTube ToS"),
        ("guaranteed viral", RiskLevel.High, "Makes unverifiable viral guarantee claims"),
        ("sub for sub", RiskLevel.Blocked, "Sub4Sub violates YouTube ToS"),
        ("sub4sub", RiskLevel.Blocked, "Sub4Sub violates YouTube ToS"),
        ("auto comment", RiskLevel.Blocked, "Automated commenting violates YouTube ToS"),
        ("auto-comment", RiskLevel.Blocked, "Automated commenting violates YouTube ToS"),
        ("comment bot", RiskLevel.Blocked, "Comment bots violate YouTube ToS"),
        (" bot ", RiskLevel.High, "Bot references may indicate ToS violations"),
        ("fake subscribers", RiskLevel.Blocked, "Fake subscribers violate YouTube ToS"),
        ("fake views", RiskLevel.Blocked, "Fake views violate YouTube ToS"),
        ("fake engagement", RiskLevel.Blocked, "Fake engagement violates YouTube ToS"),
        ("mass dm", RiskLevel.Blocked, "Mass DM spam violates platform ToS"),
        ("mass direct message", RiskLevel.Blocked, "Mass DM spam violates platform ToS"),
        ("spam", RiskLevel.High, "Spamming violates platform ToS"),
        ("world's best pianist", RiskLevel.Medium, "Unverifiable superlative claim - only use if user provided this credential"),
        ("world's greatest pianist", RiskLevel.Medium, "Unverifiable superlative claim"),
        ("#1 pianist", RiskLevel.Medium, "Unverifiable ranking claim"),
        ("best pianist in the world", RiskLevel.Medium, "Unverifiable superlative claim"),
        ("view exchange", RiskLevel.Blocked, "View exchange schemes violate YouTube ToS"),
        ("like exchange", RiskLevel.Blocked, "Like exchange schemes violate YouTube ToS"),
    };

    public ComplianceSummaryDto Review(string text)
    {
        return ReviewAll(new[] { text });
    }

    public ComplianceSummaryDto ReviewAll(IEnumerable<string> texts)
    {
        var combinedText = string.Join(" ", texts).ToLowerInvariant();
        var issues = new List<string>();
        var highestRisk = RiskLevel.Low;

        foreach (var (pattern, riskLevel, issue) in Rules)
        {
            if (combinedText.Contains(pattern.ToLowerInvariant()))
            {
                if (!issues.Contains(issue))
                {
                    issues.Add(issue);
                }

                highestRisk = EscalateRisk(highestRisk, riskLevel);
            }
        }

        return new ComplianceSummaryDto
        {
            RiskLevel = highestRisk,
            Issues = issues.ToArray(),
            IsSafeToUse = highestRisk != RiskLevel.Blocked && highestRisk != RiskLevel.High
        };
    }

    private static string EscalateRisk(string current, string candidate)
    {
        var order = new[] { RiskLevel.Low, RiskLevel.Medium, RiskLevel.High, RiskLevel.Blocked };
        var currentIdx = Array.IndexOf(order, current);
        var candidateIdx = Array.IndexOf(order, candidate);
        return candidateIdx > currentIdx ? candidate : current;
    }
}
