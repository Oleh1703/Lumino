using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class Streak_ScenesByTopic_VocabularyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Definition",
                table: "VocabularyItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExamplesJson",
                table: "VocabularyItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdiomsJson",
                table: "VocabularyItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartOfSpeech",
                table: "VocabularyItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SynonymsJson",
                table: "VocabularyItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TopicId",
                table: "Scenes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserDailyActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DateUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDailyActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDailyActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    BestStreak = table.Column<int>(type: "int", nullable: false),
                    LastActivityDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStreaks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyActivities_DateUtc",
                table: "UserDailyActivities",
                column: "DateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyActivities_UserId_DateUtc",
                table: "UserDailyActivities",
                columns: new[] { "UserId", "DateUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStreaks_LastActivityDateUtc",
                table: "UserStreaks",
                column: "LastActivityDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserStreaks_UserId",
                table: "UserStreaks",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDailyActivities");

            migrationBuilder.DropTable(
                name: "UserStreaks");

            migrationBuilder.DropColumn(
                name: "Definition",
                table: "VocabularyItems");

            migrationBuilder.DropColumn(
                name: "ExamplesJson",
                table: "VocabularyItems");

            migrationBuilder.DropColumn(
                name: "IdiomsJson",
                table: "VocabularyItems");

            migrationBuilder.DropColumn(
                name: "PartOfSpeech",
                table: "VocabularyItems");

            migrationBuilder.DropColumn(
                name: "SynonymsJson",
                table: "VocabularyItems");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "Scenes");
        }
    }
}
