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
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost,1433;Database=PianoPromoCopilot;User Id=sa;Password=PianoPromo@2024;TrustServerCertificate=True;";
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
            services.AddHttpClient<OpenAiLlmService>();
            services.AddScoped<ILlmService, OpenAiLlmService>();
        }

        // YouTube Service - use mock by default
        var useMockYouTube = configuration.GetValue<bool>("Features:UseMockYouTube", true);
        if (useMockYouTube)
        {
            services.AddSingleton<IYouTubeService, MockYouTubeService>();
        }
        else
        {
            services.AddHttpClient<GoogleYouTubeService>();
            services.AddScoped<IYouTubeService, GoogleYouTubeService>();
        }

        // Application Services
        services.AddScoped<IComplianceReviewService, ComplianceReviewService>();
        services.AddScoped<IAnalyticsRecommendationService, AnalyticsRecommendationService>();
        services.AddScoped<IVideoOptimizationService, VideoOptimizationService>();

        return services;
    }
}
