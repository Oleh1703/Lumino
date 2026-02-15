using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneCourseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Scenes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_CourseId",
                table: "Scenes",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scenes_Courses_CourseId",
                table: "Scenes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scenes_Courses_CourseId",
                table: "Scenes");

            migrationBuilder.DropIndex(
                name: "IX_Scenes_CourseId",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Scenes");
        }
    }
}
