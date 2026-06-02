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
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(responseJson);
        var messageContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return messageContent ?? throw new InvalidOperationException("LLM returned empty content");
    }
}
