using Microsoft.OpenApi.Models;
using PianoPromoCopilot.Api.Middleware;
using PianoPromoCopilot.Infrastructure;
using PianoPromoCopilot.Infrastructure.Data;
using Serilog;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .WriteTo.Console());

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PianoPromoCopilot API",
            Version = "v1",
            Description = "YouTube promotion copilot for independent pianists.\n\n" +
                          "COMPLIANCE: This API generates human-reviewable content only.\n" +
                          "No fake views, bots, or engagement manipulation.\n" +
                          "Every suggested action requires human review before use.",
            Contact = new OpenApiContact
            {
                Name = "PianoPromoCopilot",
                Url = new Uri("https://github.com/your-repo/piano-promo-copilot")
            }
        });
    });

    // CORS - allow Angular dev server
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularDev", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:4200",
                    "http://localhost:4201",
                    "http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // Infrastructure (DB, LLM, YouTube, Services)
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    // Error handling
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PianoPromoCopilot API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "PianoPromoCopilot API";
    });

    app.UseCors("AllowAngularDev");
    app.UseAuthorization();
    app.MapControllers();

    // Redirect root to swagger for convenience
    app.MapGet("/", () => Results.Redirect("/swagger"));

    // Run DB migration and seed on startup
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbSeeder.SeedAsync(dbContext, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed. Ensure SQL Server is running. Continuing without DB seed.");
        }
    }

    Log.Information("PianoPromoCopilot API starting");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
