using Microsoft.EntityFrameworkCore;
using Bot.Models;

namespace Bot.Data;
public class ApplicationContext : DbContext
{
    public DbSet<User> users {get;set;} = null!;
    public DbSet<Barber> barbers {get;set;} = null!;
    public DbSet<Books> books {get;set;} = null!;
    public DbSet<Admin> admins {get;set;} = null!;
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base (options)
    {
        //Database.EnsureCreated();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Barber>().HasData(
            new Barber {Id = 1, Name = "Светлана", special = "✂️ Мужская стрижка"},
            new Barber {Id = 2, Name = "Анастасия", special = "🎨 Окрашивание"},
            new Barber {Id = 3, Name = "Алина", special = "💇‍♀️ Женская стрижка"},
            new Barber {Id = 4, Name = "Людмила", special = "💆‍♂️ Уход за волосами"}
        );
        modelBuilder.Entity<Admin>().HasData(
            new Admin {id = 1, name = "admin", password = "1234"}
        );
    }
}