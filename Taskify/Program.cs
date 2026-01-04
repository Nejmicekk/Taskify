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

// 3. Registrace Identity (User + Role + UI + Tokeny)
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
    .AddDefaultUI();            // Důležité, aby fungovaly i stránky, které jsi nevygeneroval (např. ForgotPassword)

// 4. Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Mapování Razor Pages
app.MapRazorPages()
    .WithStaticAssets();

app.Run();