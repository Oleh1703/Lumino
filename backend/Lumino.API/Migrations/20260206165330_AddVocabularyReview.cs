using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularyReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewAt",
                table: "UserVocabularies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "UserVocabularies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextReviewAt",
                table: "UserVocabularies");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "UserVocabularies");
        }
    }
}
