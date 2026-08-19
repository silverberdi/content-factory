using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentFactory.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryboardProductionPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "storyboard_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    TotalEstimatedDurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    TotalFrameCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storyboard_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "storyboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    SupersededAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReconciledFromStoryboardId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TargetDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalEstimatedDurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SubmittedForReviewAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedForReviewByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storyboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "asset_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_plans_storyboards_StoryboardId",
                        column: x => x.StoryboardId,
                        principalTable: "storyboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "storyboard_frames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    ScriptSceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptSceneOrderIndex = table.Column<int>(type: "integer", nullable: false),
                    FramingIntent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CompositionIntent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CameraMotionIntent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Environment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    StyleIntent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    VisualPrompt = table.Column<string>(type: "text", nullable: false),
                    NegativePrompt = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    AudioCue = table.Column<string>(type: "text", nullable: false),
                    EstimatedDurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    OnScreenText = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TransitionIntent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storyboard_frames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_storyboard_frames_storyboards_StoryboardId",
                        column: x => x.StoryboardId,
                        principalTable: "storyboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_requirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameId = table.Column<Guid>(type: "uuid", nullable: true),
                    FrameOrderIndex = table.Column<int>(type: "integer", nullable: true),
                    AssetType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AspectRatio = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VisualPrompt = table.Column<string>(type: "text", nullable: false),
                    NegativePrompt = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    StyleIntent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    MotionIntent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TargetDurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    VoiceIntent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    MusicMood = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SoundEffectIntent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SubtitleProfile = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OverlaySpecification = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_requirements_asset_plans_AssetPlanId",
                        column: x => x.AssetPlanId,
                        principalTable: "asset_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_plans_ContentItemId",
                table: "asset_plans",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_asset_plans_StoryboardId",
                table: "asset_plans",
                column: "StoryboardId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_requirements_AssetPlanId",
                table: "asset_requirements",
                column: "AssetPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_asset_requirements_FrameId",
                table: "asset_requirements",
                column: "FrameId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboard_frames_ScriptSceneId",
                table: "storyboard_frames",
                column: "ScriptSceneId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboard_frames_StoryboardId",
                table: "storyboard_frames",
                column: "StoryboardId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboard_frames_StoryboardId_OrderIndex",
                table: "storyboard_frames",
                columns: new[] { "StoryboardId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_storyboard_versions_ContentItemId",
                table: "storyboard_versions",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboard_versions_StoryboardId",
                table: "storyboard_versions",
                column: "StoryboardId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboard_versions_StoryboardId_VersionNumber",
                table: "storyboard_versions",
                columns: new[] { "StoryboardId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_storyboards_ChannelId",
                table: "storyboards",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboards_ContentItemId",
                table: "storyboards",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboards_ContentItemId_IsCurrent",
                table: "storyboards",
                columns: new[] { "ContentItemId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_storyboards_ContentItemId_Status",
                table: "storyboards",
                columns: new[] { "ContentItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_storyboards_ScriptId",
                table: "storyboards",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboards_ScriptVersionId",
                table: "storyboards",
                column: "ScriptVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboards_TruthSourceId",
                table: "storyboards",
                column: "TruthSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_storyboards_TruthSourceVersionId",
                table: "storyboards",
                column: "TruthSourceVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_requirements");

            migrationBuilder.DropTable(
                name: "storyboard_frames");

            migrationBuilder.DropTable(
                name: "storyboard_versions");

            migrationBuilder.DropTable(
                name: "asset_plans");

            migrationBuilder.DropTable(
                name: "storyboards");
        }
    }
}
