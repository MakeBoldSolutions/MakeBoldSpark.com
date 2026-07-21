using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MakeBoldSpark.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBoldFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoldInstallTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoldInstallTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoldRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    InstallTokenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelRole = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Workflow = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StarterId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RunLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Retries = table.Column<int>(type: "INTEGER", nullable: false),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "TEXT", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ProviderRequestId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoldRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoldInstallTokens_TokenHash",
                table: "BoldInstallTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoldRuns_InstallTokenId",
                table: "BoldRuns",
                column: "InstallTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_BoldRuns_InstallTokenId_CreatedAt",
                table: "BoldRuns",
                columns: new[] { "InstallTokenId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BoldRuns_InstallTokenId_Workflow",
                table: "BoldRuns",
                columns: new[] { "InstallTokenId", "Workflow" });

            migrationBuilder.CreateIndex(
                name: "IX_BoldRuns_InstallTokenId_WorkspaceId",
                table: "BoldRuns",
                columns: new[] { "InstallTokenId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_BoldRuns_RunId",
                table: "BoldRuns",
                column: "RunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoldInstallTokens");

            migrationBuilder.DropTable(
                name: "BoldRuns");
        }
    }
}
