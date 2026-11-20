using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobSearchAssistant.Web.Migrations;

/// <inheritdoc />
public partial class _20261120041706_AddOpportunityComments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Comments",
            table: "JobOpportunities",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Comments",
            table: "JobOpportunities");
    }
}
