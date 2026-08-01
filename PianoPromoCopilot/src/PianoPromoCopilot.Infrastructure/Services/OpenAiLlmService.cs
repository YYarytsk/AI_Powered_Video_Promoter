using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PianoPromoCopilot.Application.Interfaces;

namespace PianoPromoCopilot.Infrastructure.Services;

/// <summary>
/// OpenAI-compatible LLM service. Supports any OpenAI-compatible endpoint.
/// Configure via OpenAI__ApiKey, OpenAI__Model, OpenAI__BaseUrl in appsettings or env vars.
/// </summary>
public class OpenAiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OpenAiLlmService> _logger;

    public OpenAiLlmService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiLlmService> logger)
    {
        _httpClient = httpClient;
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _logger = logger;

        var apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        var baseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";

        _httpClient.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.7,
            max_tokens = 4096,
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling OpenAI API with model {Model}", _model);

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // EnsureSuccessStatusCode() discards the body, and the caller silently falls back to
            // mock content on any exception - so an invalid key or an exhausted quota used to look
            // exactly like a working install producing generic text. Keep the reason.
            var detail = Truncate(responseJson, 500);
            _logger.LogError(
                "OpenAI request failed with {StatusCode} ({Reason}). Response: {Detail}",
                (int)response.StatusCode, response.ReasonPhrase, detail);

            throw new HttpRequestException(
                $"OpenAI request failed with status {(int)response.StatusCode} {response.ReasonPhrase}: {detail}",
                inner: null,
                statusCode: response.StatusCode);
        }

        try
        {
            using var doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    $"OpenAI response contained no choices. Response: {Truncate(responseJson, 500)}");
            }

            var messageContent = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                throw new InvalidOperationException("OpenAI returned empty content");
            }

            return messageContent;
        }
        catch (JsonException ex)
        {
            // A malformed envelope is a different failure from malformed generated JSON, and the
            // caller cannot tell them apart from a bare JsonException.
            throw new InvalidOperationException(
                $"OpenAI returned a response that is not valid JSON: {Truncate(responseJson, 500)}", ex);
        }
    }

    private static string Truncate(string value, int max) =>
        string.IsNullOrEmpty(value) ? "(empty)" :
        value.Length <= max ? value : value[..max] + "...";
}
