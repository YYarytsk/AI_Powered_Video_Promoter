using PianoPromoCopilot.Application.DTOs;
using PianoPromoCopilot.Application.Interfaces;

namespace PianoPromoCopilot.Application.Services;

public class AnalyticsRecommendationService : IAnalyticsRecommendationService
{
    public IReadOnlyList<AnalyticsRecommendationDto> GenerateRecommendations(
        IReadOnlyList<AnalyticsSnapshotDto> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return new[]
            {
                new AnalyticsRecommendationDto
                {
                    RecommendationType = "InitialPromotion",
                    Message = "No analytics data yet - generate your first promotion drafts",
                    Reason = "Your video needs initial promotion to start gathering viewership data",
                    SuggestedAction = "Use the Promote tab to generate social media posts and share your video on relevant communities",
                    Priority = 1
                }
            };
        }

        var recommendations = new List<AnalyticsRecommendationDto>();
        var latest = snapshots.OrderByDescending(s => s.SnapshotDate).First();
        var ordered = snapshots.OrderBy(s => s.SnapshotDate).ToList();

        // Views increasing but likes/comments low → call-to-action
        if (snapshots.Count >= 2)
        {
            var prev = ordered[ordered.Count - 2];
            var curr = ordered[ordered.Count - 1];
            bool viewsIncreasing = (curr.Views ?? 0) > (prev.Views ?? 0);
            bool lowEngagement = (curr.Likes ?? 0) < (curr.Views ?? 1) * 0.02m;

            if (viewsIncreasing && lowEngagement)
            {
                recommendations.Add(new AnalyticsRecommendationDto
                {
                    RecommendationType = "CallToAction",
                    Message = "Views are growing but engagement (likes/comments) is low",
                    Reason = "Low like-to-view ratio suggests viewers aren't being prompted to engage",
                    SuggestedAction = "Add a spoken or on-screen call-to-action early in the video asking viewers to like and comment with their favorite moment",
                    Priority = 1
                });
            }
        }

        // High avg view duration but low CTR → improve title/thumbnail
        if (latest.AverageViewDurationSeconds.HasValue && latest.ImpressionClickThroughRate.HasValue)
        {
            bool highRetention = latest.AverageViewDurationSeconds > 120;
            bool lowCtr = latest.ImpressionClickThroughRate < 0.03m;

            if (highRetention && lowCtr)
            {
                recommendations.Add(new AnalyticsRecommendationDto
                {
                    RecommendationType = "TitleThumbnailImprovement",
                    Message = "Viewers who click stay engaged, but not enough are clicking",
                    Reason = $"Average view duration is good ({latest.AverageViewDurationSeconds}s) but CTR is {latest.ImpressionClickThroughRate:P1}",
                    SuggestedAction = "Run the Optimize tool to generate improved title and thumbnail ideas. Test a more evocative title that describes the emotional quality of the piece",
                    Priority = 2
                });
            }
        }

        // High CTR but low retention → improve intro
        if (latest.ImpressionClickThroughRate.HasValue && latest.AverageViewPercentage.HasValue)
        {
            bool highCtr = latest.ImpressionClickThroughRate > 0.05m;
            bool lowRetention = latest.AverageViewPercentage < 0.30m;

            if (highCtr && lowRetention)
            {
                recommendations.Add(new AnalyticsRecommendationDto
                {
                    RecommendationType = "IntroImprovement",
                    Message = "High click-through but viewers are leaving early",
                    Reason = $"CTR is {latest.ImpressionClickThroughRate:P1} but average view percentage is only {latest.AverageViewPercentage:P1}",
                    SuggestedAction = "Improve your video intro: start with the most compelling musical moment in the first 15 seconds, then return to the beginning of the piece",
                    Priority = 1
                });
            }
        }

        // Country performance → localized posts
        if (!string.IsNullOrEmpty(latest.Country) && latest.Country != "US")
        {
            recommendations.Add(new AnalyticsRecommendationDto
            {
                RecommendationType = "Localization",
                Message = $"Strong performance in {latest.Country}",
                Reason = $"Significant views from {latest.Country} suggest a receptive audience there",
                SuggestedAction = $"Consider adding a line in your video description in the primary language of {latest.Country}. Research relevant piano communities in that region",
                Priority = 3
            });
        }

        // Traffic source: search → SEO titles
        if (!string.IsNullOrEmpty(latest.TrafficSource) &&
            latest.TrafficSource.Equals("YT_SEARCH", StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add(new AnalyticsRecommendationDto
            {
                RecommendationType = "SeoOptimization",
                Message = "Search traffic is a strong source for your video",
                Reason = "YouTube search is driving discovery",
                SuggestedAction = "Use the Optimize tool to generate SEO-optimized title variations. Research what search terms piano listeners use (e.g., 'relaxing piano music', 'original piano composition')",
                Priority = 2
            });
        }

        // Traffic source: external → double down on social
        if (!string.IsNullOrEmpty(latest.TrafficSource) &&
            latest.TrafficSource.Equals("EXT_URL", StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add(new AnalyticsRecommendationDto
            {
                RecommendationType = "SocialAmplification",
                Message = "External links are driving significant traffic",
                Reason = "Social shares and external links are working well for discovery",
                SuggestedAction = "Double down on social sharing. Use the Promotions tab to generate fresh posts for the platforms that are sending traffic. Engage with communities where your links have been shared",
                Priority = 2
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new AnalyticsRecommendationDto
            {
                RecommendationType = "Continue",
                Message = "Performance looks stable",
                Reason = "Analytics appear within expected ranges",
                SuggestedAction = "Continue posting consistently. Use the Promotions tab to generate new social posts for this video and engage authentically with your audience",
                Priority = 3
            });
        }

        return recommendations.OrderBy(r => r.Priority).ToList();
    }
}
