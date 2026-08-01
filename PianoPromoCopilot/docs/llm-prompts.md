# PianoPromoCopilot - LLM Prompts Documentation

## Overview

PianoPromoCopilot uses an OpenAI-compatible chat completion API for video optimization.
When no API key is configured, a deterministic mock LLM service is used.

---

## Video Optimization System Prompt

```
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
```

---

## Video Optimization User Prompt Template

```
Generate YouTube optimization metadata for this piano video:

Title: {title}
Description: {description}
Composition Name: {compositionName}
Mood: {mood}
Style: {style}
Tempo: {tempo}
Key Signature: {keySignature}
Target Audience: {targetAudience}
Story Behind Composition: {storyBehindComposition}
Current Tags: {currentTags}
Video URL: {videoUrl}

Generate:
- 5 alternative title options (engaging, accurate, discoverable)
- 3 description options (include the composition story, mood, relevant keywords)
- 20 relevant tags (mix of broad and specific piano/music terms)
- 10 hashtags (for YouTube and social media)
- 3 thumbnail ideas (visual concepts that convey the music's mood)
- 2 Shorts ideas (compelling 60-second clips from the composition)
- Social posts for all platforms (Instagram, TikTok, Facebook, Reddit, X, LinkedIn, Email Newsletter)

All content must be authentic, accurate to the video, and compliant with YouTube Terms of Service.
```

---

## Compliance Rules

`ComplianceReviewService` lowercases all generated text, joins it into one string, and scans
it for the patterns below. The highest matching risk level wins.

> **`src/PianoPromoCopilot.Application/Services/ComplianceReviewService.cs` is the source of
> truth for this rule set.** The table is a summary and may lag behind the code as rules are
> added - check the file if the exact list matters.

### Substring rules

| Pattern | Risk Level | Reason |
|---------|-----------|--------|
| `buy views` | Blocked | YouTube ToS violation |
| `buy likes` | Blocked | YouTube ToS violation |
| `buy subscribers` | Blocked | YouTube ToS violation |
| `buy comments` | Blocked | YouTube ToS violation |
| `guaranteed viral` | High | Unverifiable claim |
| `sub for sub` | Blocked | YouTube ToS violation |
| `sub4sub` | Blocked | YouTube ToS violation |
| `auto comment` | Blocked | YouTube ToS violation |
| `auto-comment` | Blocked | YouTube ToS violation |
| `comment bot` | Blocked | YouTube ToS violation |
| `fake subscribers` | Blocked | YouTube ToS violation |
| `fake views` | Blocked | YouTube ToS violation |
| `fake likes` | Blocked | YouTube ToS violation |
| `fake engagement` | Blocked | YouTube ToS violation |
| `mass dm` | Blocked | Platform ToS violation |
| `mass direct message` | Blocked | Platform ToS violation |
| `spam` | High | Platform ToS violation |
| `world's best pianist` | Medium | Unverifiable superlative |
| `world's greatest pianist` | Medium | Unverifiable superlative |
| `#1 pianist` | Medium | Unverifiable ranking |
| `best pianist in the world` | Medium | Unverifiable superlative |
| `view exchange` | Blocked | YouTube ToS violation |
| `like exchange` | Blocked | YouTube ToS violation |

### Word-boundary rules

Some words need boundary matching so ordinary words are not flagged.

| Pattern | Risk Level | Reason |
|---------|-----------|--------|
| `\bbots?\b` | High | Bot references may indicate ToS violations. Matches `bot`/`bots` as standalone words without flagging "robot", "bottom", "sabotage" or "both" |

### Risk Level Definitions

- **Low**: Content is safe to use after human review
- **Medium**: Content has questionable claims - review carefully and edit before use
- **High**: Content contains potentially ToS-violating content - do not use without significant editing
- **Blocked**: Content must not be used - contains clear ToS violations

`IsSafeToUse` in the response is `false` for both **High** and **Blocked**.

---

## Mock LLM Service

When `OpenAI__ApiKey` is blank or still a placeholder starting with `your-` (the shipped
default), `MockLlmService` returns a deterministic JSON response with generic piano music
optimization content.

The VideoOptimizationService also has a built-in fallback that generates video-specific mock
content using the actual title, mood, and style fields from the request.

This ensures the entire optimization workflow can be tested without any API keys.

---

## Adding OpenAI

1. Get an API key from https://platform.openai.com
2. Set in appsettings.json: `"OpenAI": { "ApiKey": "sk-..." }`
3. Or set environment variable: `OpenAI__ApiKey=sk-...`
4. The app will automatically switch to real LLM mode
5. Recommended model: `gpt-4o-mini` (good quality, low cost)
6. Alternative: any OpenAI-compatible API (Anthropic via proxy, Ollama, etc.)

---

## Prompt Engineering Notes

### Title Generation
- Target YouTube search discoverability
- Include composition name prominently
- Avoid misleading descriptors
- Use emotional descriptors that match the actual mood

### Description Generation
- Include composition backstory if provided
- Add relevant keywords naturally
- Include subscribe call-to-action
- Add timestamp markers if possible

### Tags
- Mix broad terms (piano music, classical piano) with specific terms
- Include composition name and style
- Include mood descriptors
- 20-30 tags is optimal for YouTube

### Shorts Ideas
- Focus on the most emotionally compelling moment
- Hook should capture attention in first 3 seconds
- Suggested timestamp helps creator clip the right section
- Caption is ready to copy for social posting
