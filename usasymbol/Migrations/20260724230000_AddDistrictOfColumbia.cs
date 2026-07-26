using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using USASymbol.Data;

#nullable disable

namespace usasymbol.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260724230000_AddDistrictOfColumbia")]
    public partial class AddDistrictOfColumbia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM [States]
                    WHERE [Slug] = N'district-of-columbia'
                )
                BEGIN
                    INSERT INTO [States]
                        ([Name], [Slug], [Abbreviation], [Capital], [Population], [FlagImageUrl], [Region], [StateHoodDate])
                    VALUES
                        (N'District of Columbia', N'district-of-columbia', N'DC', N'Washington, D.C.', 689545,
                         N'/images/states/flags/medium/dc.webp', N'South', NULL);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [States]
                WHERE [Slug] = N'district-of-columbia';
                """);
        }
    }
}
