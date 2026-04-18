using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Taskify.Data;
using Taskify.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Taskify.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Načtení cesty k db
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Registrace db (ApplicationDbContext)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 3. Registrace Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true; // musí obsahovat aspoň 1 číslo
        options.Password.RequiredLength = 8; // minimum je 8 znaků (NÚKIB doporučuje 12, ale tohle staci)
        options.Password.RequireNonAlphanumeric = true;// musí obsahovat speciální znak
        options.Password.RequireUppercase = true; // musi obsahovat velke písmeno
        options.Password.RequireLowercase = true; // musi obsahovat male písmeno
        
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Na jak dlouho se účet zamkne
        options.Lockout.MaxFailedAccessAttempts = 5;                       // Po kolika špatných pokusech
        options.Lockout.AllowedForNewUsers = true;                         // Platí i pro nově registrované
        
        options.SignIn.RequireConfirmedAccount = true; // overeni uctu povinne
        options.User.RequireUniqueEmail = true; // 1 email jen 1x v db
    })
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<Taskify.Services.CzechIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// 4. Registrace razor pages
builder.Services.AddRazorPages(options =>
{
    // Toto zamkne celou aplikaci, přístup mají jen přihlášení.
    // Pro Guesta je potřeba nastavit na konkrétních stránkách [AllowAnonymous]
    options.Conventions.AuthorizeFolder("/");
});

// 5. Registrace seederu
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILevelingService, LevelingService>();

var app = builder.Build();

// 5. SEEDOVÁNÍ
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RoleSeeder.SeedRolesAsync(services);
        await CategorySeeder.SeedCategoriesAsync(services);
        await AchievementSeeder.SeedAchievementsAsync(services);
        await DatabaseSeeder.SeedDatabaseAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating roles (Seeding)");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 6. BEZPEČNOSTNÍ HLAVIČKY (CSP)
app.Use(async (context, next) =>
{
    // Content Security Policy - povolí potřebné externí zdroje (mapy, fonty, CDN) a eval pro Leaflet/Chart.js
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://unpkg.com https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://unpkg.com https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https://unpkg.com https://*.tile.openstreetmap.org https://images.unsplash.com https://*.basemaps.cartocdn.com; " +
        "connect-src 'self' https://nominatim.openstreetmap.org https://unpkg.com;");
    await next();
});

// Cesta ke složce uploads
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");

// Pokud neexistuje, tak je vytvořena
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Vše co začíná na /uploads se bude hledat fyzicky ve složce uploads a nebude se ptát na manifestu
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// Ohlídání . v desetinných číslech
var supportedCultures = new[] { 
    new System.Globalization.CultureInfo("cs-CZ"), 
    new System.Globalization.CultureInfo("en-US") 
};

foreach (var c in supportedCultures)
{
    c.NumberFormat.NumberDecimalSeparator = ".";
    c.NumberFormat.CurrencyDecimalSeparator = ".";
}

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("cs-CZ"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

app.UseRequestLocalization(localizationOptions);

app.UseStaticFiles();

app.UseRouting();

// Bez 'UseAuthentication' by aplikace nevěděla, že je někdo přihlášený, i kdyby zadal správné heslo.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();