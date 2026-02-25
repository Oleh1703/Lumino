using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumino.API.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularyItemTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VocabularyItemTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VocabularyItemId = table.Column<int>(type: "int", nullable: false),
                    Translation = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyItemTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyItemTranslations_VocabularyItems_VocabularyItemId",
                        column: x => x.VocabularyItemId,
                        principalTable: "VocabularyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItemTranslations_VocabularyItemId_Order",
                table: "VocabularyItemTranslations",
                columns: new[] { "VocabularyItemId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItemTranslations_VocabularyItemId_Translation",
                table: "VocabularyItemTranslations",
                columns: new[] { "VocabularyItemId", "Translation" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularyItemTranslations");
        }
    }
}
