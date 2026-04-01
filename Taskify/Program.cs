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
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 4;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        
        options.SignIn.RequireConfirmedAccount = true;
        
        options.User.RequireUniqueEmail = true;
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

// 5. Registrace email senderu
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// 5. SEEDOVÁNÍ
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RoleSeeder.SeedRolesAsync(services);
        await CategorySeeder.SeedCategoriesAsync(services);
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