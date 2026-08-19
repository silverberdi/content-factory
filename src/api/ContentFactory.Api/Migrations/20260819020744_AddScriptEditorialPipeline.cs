using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentFactory.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptEditorialPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "script_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentIdeaVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PacingWpm = table.Column<int>(type: "integer", nullable: false),
                    EstimatedDurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    TotalWordCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_script_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "scripts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentIdeaVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TargetDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    PacingWpm = table.Column<int>(type: "integer", nullable: false),
                    EstimatedDurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    TotalWordCount = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
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
                    table.PrimaryKey("PK_scripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "script_scenes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    SceneType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NarrationText = table.Column<string>(type: "text", nullable: false),
                    VisualPrompt = table.Column<string>(type: "text", nullable: false),
                    EstimatedDurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    WordCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_script_scenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_script_scenes_scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "script_scene_evidence_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScriptSceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    TruthSourceClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimStatement = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    EditorialNote = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_script_scene_evidence_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_script_scene_evidence_references_script_scenes_ScriptSceneId",
                        column: x => x.ScriptSceneId,
                        principalTable: "script_scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_script_scene_evidence_references_ScriptSceneId",
                table: "script_scene_evidence_references",
                column: "ScriptSceneId");

            migrationBuilder.CreateIndex(
                name: "IX_script_scene_evidence_references_TruthSourceClaimId",
                table: "script_scene_evidence_references",
                column: "TruthSourceClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_script_scenes_ScriptId",
                table: "script_scenes",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_script_scenes_ScriptId_OrderIndex",
                table: "script_scenes",
                columns: new[] { "ScriptId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_script_versions_ContentItemId",
                table: "script_versions",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_script_versions_ScriptId",
                table: "script_versions",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_script_versions_ScriptId_VersionNumber",
                table: "script_versions",
                columns: new[] { "ScriptId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scripts_ChannelId",
                table: "scripts",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_scripts_ContentIdeaId",
                table: "scripts",
                column: "ContentIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_scripts_ContentIdeaVersionId",
                table: "scripts",
                column: "ContentIdeaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_scripts_ContentItemId",
                table: "scripts",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_scripts_ContentItemId_Status",
                table: "scripts",
                columns: new[] { "ContentItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_scripts_TruthSourceId",
                table: "scripts",
                column: "TruthSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_scripts_TruthSourceVersionId",
                table: "scripts",
                column: "TruthSourceVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "script_scene_evidence_references");

            migrationBuilder.DropTable(
                name: "script_versions");

            migrationBuilder.DropTable(
                name: "script_scenes");

            migrationBuilder.DropTable(
                name: "scripts");
        }
    }
}
