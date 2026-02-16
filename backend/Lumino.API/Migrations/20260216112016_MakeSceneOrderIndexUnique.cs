using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class MakeSceneOrderIndexUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scenes_CourseId_Order",
                table: "Scenes");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_CourseId_Order",
                table: "Scenes",
                columns: new[] { "CourseId", "Order" },
                unique: true,
                filter: "[Order] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scenes_CourseId_Order",
                table: "Scenes");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_CourseId_Order",
                table: "Scenes",
                columns: new[] { "CourseId", "Order" });
        }
    }
}
