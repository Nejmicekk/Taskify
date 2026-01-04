using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Taskify.Models;

namespace Taskify.Data;

// Dědíme z IdentityDbContext<User>, aby to umělo pracovat s naším "předělaným" uživatelem
public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    // Tabulka Tasks s objekty TaskItem
    public DbSet<TaskItem> Tasks { get; set; }
    // Tabulka Categories s objekty Category
    public DbSet<Category> Categories { get; set; }

    // Při každém tvoření nového objektu/modelu se nám automaticky importuje do naší db
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TaskItem>().ToTable("Tasks");
        builder.Entity<Category>().ToTable("Categories");

        // Tady by jmse mohli nastavit vazby, kdyby to EF Core nepochopil z modelů.
        // Protože jsme použili [InverseProperty] a [ForeignKey] přímo v modelech, tak EF Core to pochopí už z toho.
    }
}