using Microsoft.EntityFrameworkCore;
using USASymbol.Data;
using USASymbol.Services;
using USASymbol.Services.Content;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Memory Cache
builder.Services.AddMemoryCache();

// SQLite Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Custom Services
builder.Services.AddScoped<IMarkdownService, MarkdownService>();
builder.Services.AddScoped<IStateService, StateService>();
builder.Services.AddScoped<ISymbolService, SymbolService>();

// Content Services
builder.Services.AddScoped<IBirdService, BirdService>();
//builder.Services.AddScoped<IFlowerService, FlowerService>();
// TODO: Add TreeService, MottoService when implemented

// Response Caching
builder.Services.AddResponseCaching();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseResponseCaching();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Custom routes
app.MapControllerRoute(
    name: "state",
    pattern: "states/{slug}",
    defaults: new { controller = "State", action = "Index" });

app.MapControllerRoute(
    name: "symbol",
    pattern: "states/{stateSlug}/{symbolType}",
    defaults: new { controller = "Symbol", action = "Detail" });

app.MapControllerRoute(
    name: "symbolListing",
    pattern: "symbols/{type}",
    defaults: new { controller = "Symbol", action = "Listing" });

app.Run();