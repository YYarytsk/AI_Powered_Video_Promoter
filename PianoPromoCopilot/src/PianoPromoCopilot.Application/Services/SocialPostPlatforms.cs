using PianoPromoCopilot.Application.DTOs;

namespace PianoPromoCopilot.Application.Services;

/// <summary>
/// The mapping from a generated <see cref="SocialPostsDto"/> to the platform names we persist.
/// Both the suggestion writer and the promotion-draft writer store rows keyed by platform, and
/// they must agree: a platform present in one list and missing from the other silently produces
/// content the other half of the UI can never show. Keeping the list here makes that impossible.
/// </summary>
public static class SocialPostPlatforms
{
    public static IReadOnlyList<(string Platform, string Text)> Enumerate(SocialPostsDto posts) =>
        new[]
        {
            ("Instagram", posts.Instagram),
            ("TikTok", posts.TikTok),
            ("Facebook", posts.Facebook),
            ("Reddit", posts.Reddit),
            ("X", posts.X),
            ("LinkedIn", posts.LinkedIn),
            ("Email", posts.EmailNewsletter),
        };

    /// <summary>Platform/text pairs that actually carry content.</summary>
    public static IEnumerable<(string Platform, string Text)> EnumerateNonEmpty(SocialPostsDto posts) =>
        Enumerate(posts).Where(p => !string.IsNullOrWhiteSpace(p.Text));
}
