namespace PianoPromoCopilot.Application.DTOs;

public class OptimizeVideoResponse
{
    public string[] Titles { get; set; } = Array.Empty<string>();
    public string[] Descriptions { get; set; } = Array.Empty<string>();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string[] Hashtags { get; set; } = Array.Empty<string>();
    public string[] ThumbnailIdeas { get; set; } = Array.Empty<string>();
    public ShortsIdeaDto[] ShortsIdeas { get; set; } = Array.Empty<ShortsIdeaDto>();
    public SocialPostsDto SocialPosts { get; set; } = new();
    public ComplianceSummaryDto Compliance { get; set; } = new();
}

public class ShortsIdeaDto
{
    public string Title { get; set; } = string.Empty;
    public string Hook { get; set; } = string.Empty;
    public string SuggestedTimestamp { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
}

public class SocialPostsDto
{
    public string Instagram { get; set; } = string.Empty;
    public string TikTok { get; set; } = string.Empty;
    public string Facebook { get; set; } = string.Empty;
    public string Reddit { get; set; } = string.Empty;
    public string X { get; set; } = string.Empty;
    public string LinkedIn { get; set; } = string.Empty;
    public string EmailNewsletter { get; set; } = string.Empty;
}

public class ComplianceSummaryDto
{
    public string RiskLevel { get; set; } = "Low";
    public string[] Issues { get; set; } = Array.Empty<string>();
    public bool IsSafeToUse { get; set; } = true;
}
