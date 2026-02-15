using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCourseCompletionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "UserCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "UserCourses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "UserCourses");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "UserCourses");
        }
    }
}
