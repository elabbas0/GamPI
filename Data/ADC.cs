using GamPI.Models;
using Microsoft.EntityFrameworkCore;

public class ADC : DbContext
{
    public ADC(DbContextOptions<ADC> options)
        : base(options) { }

    public DbSet<Game> Games { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>()
            .HasIndex(g => g.Title);

        modelBuilder.Entity<Game>()
            .HasIndex(g => g.Genre);

        modelBuilder.Entity<Game>()
            .HasIndex(g => g.Developer);
        // Add indexes for commonly queried fields in the Game entity


        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique(); // Ensure usernames are unique

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique(); // Ensure emails are unique
    }
    

}