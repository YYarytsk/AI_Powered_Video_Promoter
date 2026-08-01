using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Application.Services;
using PianoPromoCopilot.Infrastructure.Data;
using PianoPromoCopilot.Infrastructure.Services;

namespace PianoPromoCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var useInMemory = configuration.GetValue<bool>("Features:UseInMemoryDatabase", false);
        if (useInMemory)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("PianoPromoCopilot"));
        }
        else
        {
            // Fail fast rather than silently falling back to a hardcoded localhost server -
            // a misspelled env var must not quietly point at the wrong database.
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No database connection string configured. Set 'ConnectionStrings:DefaultConnection' " +
                    "(env var ConnectionStrings__DefaultConnection), or set 'Features:UseInMemoryDatabase' " +
                    "to true to run without a database.");
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        services.AddScoped<IAppDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        // LLM Service - use mock if no API key configured
        var openAiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(openAiKey) || openAiKey.StartsWith("your-"))
        {
            services.AddSingleton<ILlmService, MockLlmService>();
        }
        else
        {
            // Typed client bound to the interface so the HttpClient is injected by the factory.
            services.AddHttpClient<ILlmService, OpenAiLlmService>();
        }

        // YouTube Service - use mock by default
        var useMockYouTube = configuration.GetValue<bool>("Features:UseMockYouTube", true);
        if (useMockYouTube)
        {
            services.AddSingleton<IYouTubeService, MockYouTubeService>();
        }
        else
        {
            // Typed client bound to the interface so the HttpClient is injected by the factory.
            services.AddHttpClient<IYouTubeService, GoogleYouTubeService>();
        }

        // Application Services
        services.AddScoped<IComplianceReviewService, ComplianceReviewService>();
        services.AddScoped<IAnalyticsRecommendationService, AnalyticsRecommendationService>();
        services.AddScoped<IVideoOptimizationService, VideoOptimizationService>();
        services.AddScoped<IPromotionDraftService, PromotionDraftService>();
        services.AddScoped<IYouTubeSyncService, YouTubeSyncService>();

        return services;
    }
}
