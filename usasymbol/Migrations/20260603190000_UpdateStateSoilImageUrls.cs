using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using USASymbol.Data;

namespace USASymbol.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260603190000_UpdateStateSoilImageUrls")]
    public partial class UpdateStateSoilImageUrls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE Symbols
                  SET Type = 'soil'
                  WHERE Type = 'state-soil';");

            migrationBuilder.Sql(
                @"UPDATE Symbols
                  SET ImageUrl = '/images/soils/' ||
                                 (SELECT Slug FROM States WHERE States.Id = Symbols.StateId) ||
                                 '/' ||
                                 (SELECT Slug FROM States WHERE States.Id = Symbols.StateId) ||
                                  '-state-soil-hero.webp'
                  WHERE Type = 'soil';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE Symbols
                  SET Type = 'state-soil'
                  WHERE Type = 'soil';");
        }
    }
}
