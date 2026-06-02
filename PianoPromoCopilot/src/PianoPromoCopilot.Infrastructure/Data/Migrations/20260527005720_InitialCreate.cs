using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PianoPromoCopilot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Issues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Approved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YouTubeChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: true),
                    ChannelId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChannelTitle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AccessTokenEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsConnected = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YouTubeChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YouTubeChannels_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "YouTubeVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YouTubeChannelId = table.Column<int>(type: "int", nullable: true),
                    YouTubeVideoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationIso8601 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrivacyStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ViewCount = table.Column<long>(type: "bigint", nullable: true),
                    LikeCount = table.Column<long>(type: "bigint", nullable: true),
                    CommentCount = table.Column<long>(type: "bigint", nullable: true),
                    CompositionName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Mood = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Style = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TargetAudience = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YouTubeVideos", x => x.Id);
                    table.UniqueConstraint("AK_YouTubeVideos_YouTubeVideoId", x => x.YouTubeVideoId);
                    table.ForeignKey(
                        name: "FK_YouTubeVideos_YouTubeChannels_YouTubeChannelId",
                        column: x => x.YouTubeChannelId,
                        principalTable: "YouTubeChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PromotionDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YouTubeVideoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DraftText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionDrafts_YouTubeVideos_YouTubeVideoId",
                        column: x => x.YouTubeVideoId,
                        principalTable: "YouTubeVideos",
                        principalColumn: "YouTubeVideoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YouTubeVideoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: true),
                    Likes = table.Column<long>(type: "bigint", nullable: true),
                    Comments = table.Column<long>(type: "bigint", nullable: true),
                    SubscribersGained = table.Column<long>(type: "bigint", nullable: true),
                    EstimatedMinutesWatched = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AverageViewDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    AverageViewPercentage = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Impressions = table.Column<long>(type: "bigint", nullable: true),
                    ImpressionClickThroughRate = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    TrafficSource = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoAnalyticsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoAnalyticsSnapshots_YouTubeVideos_YouTubeVideoId",
                        column: x => x.YouTubeVideoId,
                        principalTable: "YouTubeVideos",
                        principalColumn: "YouTubeVideoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoOptimizationSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YouTubeVideoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SuggestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SuggestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoOptimizationSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoOptimizationSuggestions_YouTubeVideos_YouTubeVideoId",
                        column: x => x.YouTubeVideoId,
                        principalTable: "YouTubeVideos",
                        principalColumn: "YouTubeVideoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionDrafts_YouTubeVideoId",
                table: "PromotionDrafts",
                column: "YouTubeVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoAnalyticsSnapshots_YouTubeVideoId",
                table: "VideoAnalyticsSnapshots",
                column: "YouTubeVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoOptimizationSuggestions_YouTubeVideoId",
                table: "VideoOptimizationSuggestions",
                column: "YouTubeVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeChannels_AppUserId",
                table: "YouTubeChannels",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeVideos_YouTubeChannelId",
                table: "YouTubeVideos",
                column: "YouTubeChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeVideos_YouTubeVideoId",
                table: "YouTubeVideos",
                column: "YouTubeVideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceReviews");

            migrationBuilder.DropTable(
                name: "PromotionDrafts");

            migrationBuilder.DropTable(
                name: "VideoAnalyticsSnapshots");

            migrationBuilder.DropTable(
                name: "VideoOptimizationSuggestions");

            migrationBuilder.DropTable(
                name: "YouTubeVideos");

            migrationBuilder.DropTable(
                name: "YouTubeChannels");

            migrationBuilder.DropTable(
                name: "AppUsers");
        }
    }
}
