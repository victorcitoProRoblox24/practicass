using Microsoft.EntityFrameworkCore;
using VulnerableApp.Models;

namespace VulnerableApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Hashes BCrypt precalculados para admin/123456/password (HasData exige valores estáticos).
            var seedDate = new DateTime(2026, 1, 1);
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", PasswordHash = "$2a$11$jgGZiGmxn.nSJRRL8Mj9c.gUd8nxPdPA8L03bfgQRXKTLPaTrGRQO", Email = "admin@test.com", Balance = 1000m, CreatedAt = seedDate },
                new User { Id = 2, Username = "user1", PasswordHash = "$2a$11$RdVRXvvth6EPbj8HD.rCeuu286h187aCj4GcEKNimPXGZ0ktVB7OC", Email = "user@test.com", Balance = 500m, CreatedAt = seedDate },
                new User { Id = 3, Username = "user2", PasswordHash = "$2a$11$5kx344RwO7EcL9.JZ/NbAOrS0gOcWiSLT5L01Rhs45WdCy0xCKzUi", Email = "user2@test.com", Balance = 750m, CreatedAt = seedDate }
            );
        }
    }
}
