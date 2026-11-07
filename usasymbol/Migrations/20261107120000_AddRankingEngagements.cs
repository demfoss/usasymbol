using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using USASymbol.Data;

#nullable disable

namespace usasymbol.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261107120000_AddRankingEngagements")]
public partial class AddRankingEngagements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RankingEngagements",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                RankingSlug = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                VisitorHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                ViewCount = table.Column<int>(type: "int", nullable: false),
                FirstViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Reaction = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                FirstReactedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastReactedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReactionChangeCount = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RankingEngagements", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RankingEngagements_Category_RankingSlug_Reaction",
            table: "RankingEngagements",
            columns: new[] { "Category", "RankingSlug", "Reaction" });

        migrationBuilder.CreateIndex(
            name: "IX_RankingEngagements_Category_RankingSlug_VisitorHash",
            table: "RankingEngagements",
            columns: new[] { "Category", "RankingSlug", "VisitorHash" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RankingEngagements_LastViewedAt",
            table: "RankingEngagements",
            column: "LastViewedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RankingEngagements");
    }
}
