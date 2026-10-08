using Bot.Data;
using Bot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bot.Services
{
    public class AdminSeeder
    {
        private readonly ApplicationContext _db;
        private readonly IConfiguration _configuration;

        public AdminSeeder(ApplicationContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task SeedAsync()
        {
            if (!await _db.admins.AnyAsync())
            {
                var admin = new Admin
                {
                    name = "admin",
                };
                var password = _configuration["Admin:Password"];

                if (string.IsNullOrEmpty(password))
                {
                    throw new Exception("Admin password is not configured");
                }

                var hasher = new PasswordHasher<Admin>();
                admin.PasswordHash = hasher.HashPassword(admin, password);
                _db.admins.Add(admin);
                await _db.SaveChangesAsync();
            }
        }
    }
}
