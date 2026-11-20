using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobSearchAssistant.Web.Migrations;

/// <inheritdoc />
public partial class _20261118093536_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "JobOpportunities",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                CompanyName = table.Column<string>(type: "TEXT", nullable: true),
                JobTitle = table.Column<string>(type: "TEXT", nullable: true),
                OriginalPostingText = table.Column<string>(type: "TEXT", nullable: false),
                CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobOpportunities", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "JobOpportunities");
    }
}
