using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonAndExerciseVocabularyRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseVocabularies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    VocabularyItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseVocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseVocabularies_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseVocabularies_VocabularyItems_VocabularyItemId",
                        column: x => x.VocabularyItemId,
                        principalTable: "VocabularyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonVocabularies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    VocabularyItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonVocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonVocabularies_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonVocabularies_VocabularyItems_VocabularyItemId",
                        column: x => x.VocabularyItemId,
                        principalTable: "VocabularyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseVocabularies_ExerciseId",
                table: "ExerciseVocabularies",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseVocabularies_ExerciseId_VocabularyItemId",
                table: "ExerciseVocabularies",
                columns: new[] { "ExerciseId", "VocabularyItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseVocabularies_VocabularyItemId",
                table: "ExerciseVocabularies",
                column: "VocabularyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonVocabularies_LessonId",
                table: "LessonVocabularies",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonVocabularies_LessonId_VocabularyItemId",
                table: "LessonVocabularies",
                columns: new[] { "LessonId", "VocabularyItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonVocabularies_VocabularyItemId",
                table: "LessonVocabularies",
                column: "VocabularyItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseVocabularies");

            migrationBuilder.DropTable(
                name: "LessonVocabularies");
        }
    }
}
