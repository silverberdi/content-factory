using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentFactory.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualAssetGenerationJobAndMediaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "generated_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryboardVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetRequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantIndex = table.Column<int>(type: "integer", nullable: false),
                    AssetType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StorageProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    ChecksumSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderModelOrWorkflow = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GenerationParametersSnapshot = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsSelectedForAssembly = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capability = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceAssetRequirementId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoryboardId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoryboardVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelOrWorkflowIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualCostUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SanitizedErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsRetryable = table.Column<bool>(type: "boolean", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "job_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ProviderResponseSummary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    EstimatedCostUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualCostUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_attempts_jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_assets_AssetRequirementId",
                table: "generated_assets",
                column: "AssetRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_assets_AssetRequirementId_IsSelectedForAssembly",
                table: "generated_assets",
                columns: new[] { "AssetRequirementId", "IsSelectedForAssembly" });

            migrationBuilder.CreateIndex(
                name: "IX_generated_assets_ContentItemId",
                table: "generated_assets",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_assets_JobId",
                table: "generated_assets",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_assets_Status",
                table: "generated_assets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_generated_assets_StoryboardId",
                table: "generated_assets",
                column: "StoryboardId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_assets_StoryboardVersionId",
                table: "generated_assets",
                column: "StoryboardVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_job_attempts_JobId",
                table: "job_attempts",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_job_attempts_JobId_AttemptNumber",
                table: "job_attempts",
                columns: new[] { "JobId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_ChannelId",
                table: "jobs",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_ContentItemId",
                table: "jobs",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_CorrelationId",
                table: "jobs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_IdempotencyKey",
                table: "jobs",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_SourceAssetRequirementId",
                table: "jobs",
                column: "SourceAssetRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_Status",
                table: "jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_StoryboardId",
                table: "jobs",
                column: "StoryboardId");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_StoryboardVersionId",
                table: "jobs",
                column: "StoryboardVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_assets");

            migrationBuilder.DropTable(
                name: "job_attempts");

            migrationBuilder.DropTable(
                name: "jobs");
        }
    }
}
