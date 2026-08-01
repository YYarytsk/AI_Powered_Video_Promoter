using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PianoPromoCopilot.Application.Interfaces;
using PianoPromoCopilot.Infrastructure;
using PianoPromoCopilot.Infrastructure.Services;
using Xunit;

namespace PianoPromoCopilot.Tests.Infrastructure;

/// <summary>
/// Mock mode has to work with no OpenAI key, no YouTube credentials, no Docker and no SQL Server.
/// That guarantee is a container-resolution guarantee, so it is worth checking here rather than
/// discovering it at the first HTTP request.
/// </summary>
public class DependencyInjectionTests
{
    private static IConfiguration MockModeConfiguration(params (string Key, string Value)[] overrides)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Features:UseInMemoryDatabase"] = "true",
            ["Features:UseMockYouTube"] = "true",
            // The placeholder that ships in appsettings.json - must select the mock LLM.
            ["OpenAI:ApiKey"] = "your-openai-api-key-here"
        };

        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // The web host registers IConfiguration itself; the HTTP-backed services depend on it.
        services.AddSingleton(configuration);
        services.AddInfrastructure(configuration);

        // validateScopes catches a scoped service captured by a singleton; validateOnBuild fails
        // fast on a registration nothing can satisfy.
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    [Fact]
    public void AddInfrastructure_MockMode_ResolvesEveryApplicationService()
    {
        using var provider = BuildProvider(MockModeConfiguration());
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<IAppDbContext>().Should().NotBeNull();
        sp.GetRequiredService<IComplianceReviewService>().Should().NotBeNull();
        sp.GetRequiredService<IAnalyticsRecommendationService>().Should().NotBeNull();
        sp.GetRequiredService<IVideoOptimizationService>().Should().NotBeNull();
        sp.GetRequiredService<IPromotionDraftService>().Should().NotBeNull();
        sp.GetRequiredService<IYouTubeSyncService>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_PlaceholderApiKeyAndMockFlag_SelectMockImplementations()
    {
        using var provider = BuildProvider(MockModeConfiguration());
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ILlmService>().Should().BeOfType<MockLlmService>();
        scope.ServiceProvider.GetRequiredService<IYouTubeService>().Should().BeOfType<MockYouTubeService>();
    }

    [Fact]
    public void AddInfrastructure_RealCredentials_ResolveHttpBackedImplementations()
    {
        // AddHttpClient<IInterface, Implementation>() is what makes these resolvable. Pairing
        // AddHttpClient<T>() with a separate AddScoped<IInterface, T>() does not, because the
        // second registration builds the type through a container that has no plain HttpClient.
        // That bug shipped once - this test is here so it cannot ship again.
        using var provider = BuildProvider(MockModeConfiguration(
            ("OpenAI:ApiKey", "sk-a-real-looking-key"),
            ("Features:UseMockYouTube", "false")));

        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ILlmService>().Should().BeOfType<OpenAiLlmService>();
        scope.ServiceProvider.GetRequiredService<IYouTubeService>().Should().BeOfType<GoogleYouTubeService>();
    }

    [Fact]
    public void AddInfrastructure_RelationalWithoutConnectionString_FailsFast()
    {
        var configuration = MockModeConfiguration(("Features:UseInMemoryDatabase", "false"));

        var act = () => BuildProvider(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*connection string*",
                "a misspelled env var must not silently fall back to a hardcoded localhost server");
    }
}
