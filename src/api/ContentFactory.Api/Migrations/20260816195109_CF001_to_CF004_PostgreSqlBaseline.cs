using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentFactory.Api.Migrations
{
    /// <inheritdoc />
    public partial class CF001_to_CF004_PostgreSqlBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    TruthSourceVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Capability = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PromptPolicyVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StructuredOutputJson = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: true),
                    Rationale = table.Column<string>(type: "text", nullable: true),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    TokensIn = table.Column<int>(type: "integer", nullable: false),
                    TokensOut = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    AcceptedState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "channels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Niche = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_idea_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Angle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    HookStrategy = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AudienceValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IntendedOutcome = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FreshnessClass = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DismissalNotes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    EditedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_idea_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_ideas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Angle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    HookStrategy = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AudienceValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IntendedOutcome = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FreshnessClass = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DismissalNotes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    SelectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SelectedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_ideas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Slug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "discovery_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscoverySourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    NormalizedUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    RawContent = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Author = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DiscoveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OriginType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubmitterEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DismissalReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    EditorialNotes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PromotedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PromotedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discovery_candidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "discovery_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OriginUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PollingIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSyncAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextSyncAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    LastErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discovery_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "editorial_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AssignedUserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_editorial_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "truth_source_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    SupportingEvidenceIdsJson = table.Column<string>(type: "text", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_truth_source_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "truth_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    KeyIdeasJson = table.Column<string>(type: "text", nullable: false),
                    VerifiableClaimsJson = table.Column<string>(type: "text", nullable: false),
                    EvidenceReferencesJson = table.Column<string>(type: "text", nullable: false),
                    RiskNotes = table.Column<string>(type: "text", nullable: false),
                    DoNotSayConstraintsJson = table.Column<string>(type: "text", nullable: false),
                    PossibleAnglesJson = table.Column<string>(type: "text", nullable: false),
                    LocalizationNotes = table.Column<string>(type: "text", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_truth_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Roles = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsOwner = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_item_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscoveryCandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: true),
                    ObjectStorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ExtractedText = table.Column<string>(type: "text", nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Author = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_item_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_item_evidence_content_items_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "content_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_Capability",
                table: "ai_recommendations",
                column: "Capability");

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_ChannelId",
                table: "ai_recommendations",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_ContentItemId",
                table: "ai_recommendations",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_TruthSourceVersionId",
                table: "ai_recommendations",
                column: "TruthSourceVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_TimestampUtc",
                table: "audit_events",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_channels_Slug",
                table: "channels",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_idea_versions_ContentIdeaId",
                table: "content_idea_versions",
                column: "ContentIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_content_idea_versions_ContentIdeaId_VersionNumber",
                table: "content_idea_versions",
                columns: new[] { "ContentIdeaId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_idea_versions_ContentItemId",
                table: "content_idea_versions",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_content_idea_versions_TruthSourceId",
                table: "content_idea_versions",
                column: "TruthSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_content_ideas_ContentItemId",
                table: "content_ideas",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_content_ideas_ContentItemId_Status",
                table: "content_ideas",
                columns: new[] { "ContentItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_content_ideas_TruthSourceId",
                table: "content_ideas",
                column: "TruthSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_content_ideas_TruthSourceVersionId",
                table: "content_ideas",
                column: "TruthSourceVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_content_item_evidence_ContentHash",
                table: "content_item_evidence",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_content_item_evidence_ContentItemId",
                table: "content_item_evidence",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_content_item_evidence_DiscoveryCandidateId",
                table: "content_item_evidence",
                column: "DiscoveryCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_content_item_evidence_Status",
                table: "content_item_evidence",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_ChannelId",
                table: "content_items",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_ChannelId_Stage",
                table: "content_items",
                columns: new[] { "ChannelId", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_content_items_ChannelId_Status",
                table: "content_items",
                columns: new[] { "ChannelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_discovery_candidates_ChannelId_NormalizedUrl",
                table: "discovery_candidates",
                columns: new[] { "ChannelId", "NormalizedUrl" });

            migrationBuilder.CreateIndex(
                name: "IX_discovery_candidates_DiscoverySourceId",
                table: "discovery_candidates",
                column: "DiscoverySourceId");

            migrationBuilder.CreateIndex(
                name: "IX_discovery_candidates_Status_DiscoveredAtUtc",
                table: "discovery_candidates",
                columns: new[] { "Status", "DiscoveredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_discovery_sources_ChannelId_OriginUrl",
                table: "discovery_sources",
                columns: new[] { "ChannelId", "OriginUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discovery_sources_Status",
                table: "discovery_sources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_editorial_tasks_ChannelId",
                table: "editorial_tasks",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_editorial_tasks_ContentItemId",
                table: "editorial_tasks",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_editorial_tasks_Status_Priority",
                table: "editorial_tasks",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_truth_source_versions_ContentItemId",
                table: "truth_source_versions",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_truth_source_versions_TruthSourceId",
                table: "truth_source_versions",
                column: "TruthSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_truth_source_versions_TruthSourceId_VersionNumber",
                table: "truth_source_versions",
                columns: new[] { "TruthSourceId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_truth_sources_ContentItemId",
                table: "truth_sources",
                column: "ContentItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_truth_sources_Status",
                table: "truth_sources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_invitations_Email",
                table: "user_invitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId_Role",
                table: "user_roles",
                columns: new[] { "UserId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_recommendations");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "channels");

            migrationBuilder.DropTable(
                name: "content_idea_versions");

            migrationBuilder.DropTable(
                name: "content_ideas");

            migrationBuilder.DropTable(
                name: "content_item_evidence");

            migrationBuilder.DropTable(
                name: "discovery_candidates");

            migrationBuilder.DropTable(
                name: "discovery_sources");

            migrationBuilder.DropTable(
                name: "editorial_tasks");

            migrationBuilder.DropTable(
                name: "truth_source_versions");

            migrationBuilder.DropTable(
                name: "truth_sources");

            migrationBuilder.DropTable(
                name: "user_invitations");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "content_items");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
