using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taskify.Data;
using Taskify.Models;

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
        // Nastavení hesla (Dev mode)
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 4;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;

        // Potvrzování emailu vypnuto
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddErrorDescriber<Taskify.Services.CzechIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders() // Důležité pro reset hesla atd.
    .AddDefaultUI();            // Důležité, aby fungovaly i stránky, které jsem automaticky nevygeneroval (např. ForgotPassword)

// 4. Razor Pages
builder.Services.AddRazorPages(options =>
{
    // Toto zamkne celou aplikaci, přístup mají jen přihlášení.
    // Pro Guesta je potřeba nastavit na konkrétních stránkách [AllowAnonymous]
    options.Conventions.AuthorizeFolder("/");
});

var app = builder.Build();

// 5. SEEDOVÁNÍ ROLÍ
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RoleSeeder.SeedRolesAsync(services);
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
app.UseRouting();

// Bez 'UseAuthentication' by aplikace nevěděla, že je někdo přihlášený, i kdyby zadal správné heslo.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();