using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace USASymbol.Data
{
    // Used only by the `dotnet ef migrations` CLI tooling at design time, so migrations are
    // always generated against SQL Server (the production provider) regardless of which
    // provider the running app is configured for locally (SQLite in Development).
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer("Server=tcp:localhost,1433;Initial Catalog=usasymbol;");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
