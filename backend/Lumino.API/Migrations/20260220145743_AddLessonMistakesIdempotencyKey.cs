using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonMistakesIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MistakesIdempotencyKey",
                table: "LessonResults",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonResults_UserId_MistakesIdempotencyKey",
                table: "LessonResults",
                columns: new[] { "UserId", "MistakesIdempotencyKey" },
                unique: true,
                filter: "[MistakesIdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonResults_UserId_MistakesIdempotencyKey",
                table: "LessonResults");

            migrationBuilder.DropColumn(
                name: "MistakesIdempotencyKey",
                table: "LessonResults");
        }
    }
}
