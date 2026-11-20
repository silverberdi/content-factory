using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentFactory.Api.Migrations;

public partial class AddProviderNatureMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsSimulated",
            table: "ai_recommendations",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsSimulated",
            table: "generated_assets",
            type: "boolean",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsSimulated", table: "ai_recommendations");
        migrationBuilder.DropColumn(name: "IsSimulated", table: "generated_assets");
    }
}
