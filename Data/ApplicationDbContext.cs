using BackToYou.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BackToYou.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<Claim> Claims => Set<Claim>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Relationships
            builder.Entity<Item>()
                .HasOne(i => i.Reporter)
                .WithMany(u => u.ReportedItems)
                .HasForeignKey(i => i.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Item>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Claim>()
                .HasOne(c => c.Item)
                .WithMany(i => i.Claims)
                .HasForeignKey(c => c.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Claim>()
                .HasOne(c => c.Claimer)
                .WithMany(u => u.SubmittedClaims)
                .HasForeignKey(c => c.ClaimerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
