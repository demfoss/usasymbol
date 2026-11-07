using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using USASymbol.Data;

#nullable disable

namespace usasymbol.Migrations
{
    /// <summary>Additive: anonymous quiz events (start / answer / complete) for question-level analytics.</summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260801090000_AddQuizEvents")]
    public partial class AddQuizEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizSlug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuestionKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    Choice = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizEvents_QuizSlug_EventType",
                table: "QuizEvents",
                columns: new[] { "QuizSlug", "EventType" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QuizEvents");
        }
    }
}
