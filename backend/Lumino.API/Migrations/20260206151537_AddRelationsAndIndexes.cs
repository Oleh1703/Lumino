using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_UserId_VocabularyItemId",
                table: "UserVocabularies",
                columns: new[] { "UserId", "VocabularyItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_VocabularyItemId",
                table: "UserVocabularies",
                column: "VocabularyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_CourseId_Order",
                table: "Topics",
                columns: new[] { "CourseId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SceneAttempts_SceneId",
                table: "SceneAttempts",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_SceneAttempts_UserId_SceneId",
                table: "SceneAttempts",
                columns: new[] { "UserId", "SceneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_TopicId_Order",
                table: "Lessons",
                columns: new[] { "TopicId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonResults_LessonId",
                table: "LessonResults",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonResults_UserId_LessonId",
                table: "LessonResults",
                columns: new[] { "UserId", "LessonId" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_LessonId",
                table: "Exercises",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Lessons_LessonId",
                table: "Exercises",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonResults_Lessons_LessonId",
                table: "LessonResults",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonResults_Users_UserId",
                table: "LessonResults",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Topics_TopicId",
                table: "Lessons",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SceneAttempts_Scenes_SceneId",
                table: "SceneAttempts",
                column: "SceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SceneAttempts_Users_UserId",
                table: "SceneAttempts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Topics_Courses_CourseId",
                table: "Topics",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId",
                principalTable: "Achievements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievements_Users_UserId",
                table: "UserAchievements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Users_UserId",
                table: "UserProgresses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserVocabularies_Users_UserId",
                table: "UserVocabularies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserVocabularies_VocabularyItems_VocabularyItemId",
                table: "UserVocabularies",
                column: "VocabularyItemId",
                principalTable: "VocabularyItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Lessons_LessonId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonResults_Lessons_LessonId",
                table: "LessonResults");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonResults_Users_UserId",
                table: "LessonResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Topics_TopicId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_SceneAttempts_Scenes_SceneId",
                table: "SceneAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_SceneAttempts_Users_UserId",
                table: "SceneAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_Topics_Courses_CourseId",
                table: "Topics");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievements_Users_UserId",
                table: "UserAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Users_UserId",
                table: "UserProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserVocabularies_Users_UserId",
                table: "UserVocabularies");

            migrationBuilder.DropForeignKey(
                name: "FK_UserVocabularies_VocabularyItems_VocabularyItemId",
                table: "UserVocabularies");

            migrationBuilder.DropIndex(
                name: "IX_UserVocabularies_UserId_VocabularyItemId",
                table: "UserVocabularies");

            migrationBuilder.DropIndex(
                name: "IX_UserVocabularies_VocabularyItemId",
                table: "UserVocabularies");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements");

            migrationBuilder.DropIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements");

            migrationBuilder.DropIndex(
                name: "IX_Topics_CourseId_Order",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_SceneAttempts_SceneId",
                table: "SceneAttempts");

            migrationBuilder.DropIndex(
                name: "IX_SceneAttempts_UserId_SceneId",
                table: "SceneAttempts");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_TopicId_Order",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_LessonResults_LessonId",
                table: "LessonResults");

            migrationBuilder.DropIndex(
                name: "IX_LessonResults_UserId_LessonId",
                table: "LessonResults");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_LessonId",
                table: "Exercises");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
