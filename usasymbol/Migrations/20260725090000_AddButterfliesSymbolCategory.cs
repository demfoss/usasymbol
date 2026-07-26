using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using USASymbol.Data;

#nullable disable

namespace usasymbol.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260725090000_AddButterfliesSymbolCategory")]
    public partial class AddButterfliesSymbolCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO [SymbolCategories] ([Type], [Name], [Description], [ImageUrl])
                SELECT 'butterflies',
                       'State Butterflies',
                       'Explore official state butterflies, including monarchs, swallowtails, fritillaries, hairstreaks, and sulphurs.',
                       '/images/insects/species/eastern-tiger-swallowtail-01.webp'
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [SymbolCategories]
                    WHERE [Type] = 'butterflies'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [SymbolCategories]
                WHERE [Type] = 'butterflies';
                """);
        }
    }
}
