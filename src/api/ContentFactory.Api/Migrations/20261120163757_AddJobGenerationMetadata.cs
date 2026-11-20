using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentFactory.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobGenerationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Capability",
                table: "jobs",
                newName: "JobType");

            migrationBuilder.AddColumn<int>(
                name: "AutomaticRetriesRemaining",
                table: "jobs",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "CandidateCount",
                table: "jobs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionParametersSnapshot",
                table: "jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GenerationRevision",
                table: "jobs",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomaticRetriesRemaining",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "CandidateCount",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "ExecutionParametersSnapshot",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "GenerationRevision",
                table: "jobs");

            migrationBuilder.RenameColumn(
                name: "JobType",
                table: "jobs",
                newName: "Capability");
        }
    }
}
