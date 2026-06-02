using Microsoft.Extensions.Logging;
using PianoPromoCopilot.Application.Interfaces;

namespace PianoPromoCopilot.Infrastructure.Services;

/// <summary>
/// Mock LLM service that returns pre-built deterministic responses.
/// Used when OpenAI API key is not configured or for testing.
/// </summary>
public class MockLlmService : ILlmService
{
    private readonly ILogger<MockLlmService> _logger;

    public MockLlmService(ILogger<MockLlmService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockLlmService: returning deterministic mock response");

        // Return a generic mock JSON - VideoOptimizationService has its own fallback builder
        // but this ensures the interface is satisfied
        var response = """
            {
              "titles": [
                "Original Piano Composition - A Musical Journey",
                "Peaceful Piano Music - Original Composition",
                "Soothing Piano Piece | Original Music",
                "Piano Composition | Relaxing Instrumental",
                "Original Piano Work - Acoustic Performance"
              ],
              "descriptions": [
                "An original piano composition crafted with care and artistic intention. This piece explores emotional themes through acoustic piano performance.\n\nSubscribe for more original music!\n\n#piano #originalmusic",
                "This original piano composition was created to evoke a sense of peace and reflection. Perfect for study, relaxation, or simply enjoying beautiful music.\n\n#pianomusic #composer #classicalpiano",
                "A heartfelt original piano composition. Every note was chosen intentionally to create an authentic musical experience.\n\nWatch, listen, and feel the music. If you enjoy this piece, please like and subscribe!"
              ],
              "tags": [
                "piano music", "original piano", "piano composition", "solo piano", "acoustic piano",
                "original music", "piano performance", "relaxing piano", "classical piano", "piano pieces",
                "indie composer", "piano instrumental", "beautiful piano music", "piano melody",
                "original composition", "peaceful music", "study music", "calm music", "piano solo", "composer"
              ],
              "hashtags": [
                "#piano", "#originalmusic", "#pianocomposition", "#classicalpiano", "#pianomusic",
                "#pianist", "#composer", "#acousticpiano", "#relaxingmusic", "#originalcomposition"
              ],
              "thumbnailIdeas": [
                "Close-up of pianist hands on keys with warm, intimate lighting",
                "Wide artistic shot of piano in natural light with dramatic shadows",
                "Abstract close-up of piano strings with soft bokeh background"
              ],
              "shortsIdeas": [
                {
                  "title": "The Most Beautiful Moment in This Piano Piece",
                  "hook": "Wait for the main theme to return at 2:30...",
                  "suggestedTimestamp": "2:30",
                  "description": "Highlighting the emotional climax of this original piano composition",
                  "caption": "Original piano composition 🎹 #piano #pianomusic #originalmusic"
                },
                {
                  "title": "Composing This Piano Piece from a Single Idea",
                  "hook": "Every composition starts with just one musical idea...",
                  "suggestedTimestamp": "0:00",
                  "description": "The creative process behind this original piano work",
                  "caption": "The story behind the music 🎹 #pianocomposer #originalmusic"
                }
              ],
              "socialPosts": {
                "instagram": "🎹 New original piano composition is live!\n\nThis piece was created with love and intention. Every phrase tells a story.\n\nLink in bio to watch the full performance 🎵\n\n#piano #originalmusic #pianocomposition #pianist #classicalpiano #composer #acousticpiano #pianomusic #relaxingmusic #originalcomposition",
                "tiktok": "POV: discovering peaceful piano music at midnight 🌙🎹\n\nNew original composition - link in bio!\n\n#piano #pianomusic #originalcomposition #classicalpiano #composer #fyp",
                "facebook": "I'm excited to share my latest original piano composition!\n\nThis piece represents hours of creative work and emotional investment. I hope it brings you a moment of peace and beauty today.\n\nWatch the full performance here: [video link]\n\nLet me know what you feel when you hear it! 🎹",
                "reddit": "I just released an original piano composition on my YouTube channel. I've been composing independently and would love feedback from this community. Here's the link if you'd like to listen: [video link]",
                "x": "New original piano composition just released 🎹\n\nFull performance on YouTube - link below.\n\n#piano #originalmusic #composer",
                "linkedin": "I recently released an original piano composition on my YouTube channel. As an independent composer and pianist, creating authentic music that connects with listeners is my primary goal. I'd be honored if you'd take a few minutes to listen: [video link]",
                "emailNewsletter": "Subject: New Original Piano Composition\n\nHello,\n\nI'm delighted to share my latest original piano composition with you.\n\nThis piece was composed to create a moment of beauty and reflection in your day.\n\n[Watch on YouTube →]\n\nYour support means the world to me.\n\nWith gratitude,\n[Your Name]"
              }
            }
            """;

        return Task.FromResult(response);
    }
}
