using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Models.Audit;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Purchase;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Data
{
    public class AppDbContext : IdentityDbContext<User, Role, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        /* =========
         * Audit
         * ========= */
        public DbSet<AlertSignalLog> AlertSignalLogs { get; set; }
        public DbSet<DbStats> DbStats { get; set; }
        public DbSet<UserLog> UserLog { get; set; }
        public DbSet<ErrorLog> ErrorLog { get; set; }
        
        /* =========
         * User
         * ========= */
        // Users and Roles provided by Identity
        public DbSet<Employee> Employees { get; set; }
        public DbSet<PostalCode> PostalCodes { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<UserCategory> UserCategories { get; set; }
        
        /* =========
         * Inventory
         * ========= */
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        
        /* =========
         * Purchase
         * ========= */
        public DbSet<BalanceCharge> BalanceCharges { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<ProductPurchase> ProductPurchases { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        
        /* =========
         * Tickets
         * ========= */
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketPrice> TicketPrices { get; set; }
        public DbSet<TicketPurchase> TicketPurchases { get; set; }
        public DbSet<TicketTransfer> TicketTransfers { get; set; }
        
        /* ==========
         * Fluent API
         * ========== */
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set up Identity tables
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.UserCategory)
                .WithMany()
                .IsRequired();
            
            modelBuilder.Entity<UserCategory>()
                .HasMany(uc => uc.TicketPrices)
                .WithOne(tp => tp.UserCategory)
                .HasForeignKey(tp => tp.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite Key for ProductPurchase
            modelBuilder.Entity<ProductPurchase>()
                .HasKey(pp => new { pp.ProductId, pp.PurchaseId });

            modelBuilder.Entity<ProductPurchase>()
                .HasOne(pp => pp.Product)
                .WithMany(p => p.ProductPurchases)
                .HasForeignKey(pp => pp.ProductId);

            modelBuilder.Entity<ProductPurchase>()
                .HasOne(pp => pp.Purchase)
                .WithMany(p => p.ProductPurchases)
                .HasForeignKey(pp => pp.PurchaseId);
            
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Owner)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict); // Prevent multiple cascade
            
            modelBuilder.Entity<TicketTransfer>()
                .HasOne(tt => tt.Sender)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketTransfer>()
                .HasOne(tt => tt.Receiver)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Global configuration for Decimals
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }

            // TPT (Table Per Type) Inheritance Mapping
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<Employee>().ToTable("Employee");
        }
    }
}