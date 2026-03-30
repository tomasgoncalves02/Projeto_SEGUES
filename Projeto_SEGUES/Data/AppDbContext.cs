using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Audit;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Data;

/// <summary>
/// The primary Database Context for the application, inheriting from IdentityDbContext 
/// to support integrated User and Role management.
/// </summary>
/// <remarks>
/// This class orchestrates the Object-Relational Mapping (ORM) for all system modules: 
/// Auditing, Administration, Inventory, Orders, Payments, and Ticketing.
/// </remarks>
public class AppDbContext : IdentityDbContext<AppUser, Role, string>
{
    /// <summary>
    /// Constructor for the AppDbContext.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext, typically configured in Program.cs.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /* =========
     * Audit
     * ========= */
    /// <summary>
    /// Database statistics for the application.
    /// </summary>
    public DbSet<DbStats> DbStats { get; set; }
    /// <summary>
    /// Audit logs for user actions and system errors.
    /// </summary>
    public DbSet<UserLog> UserLog { get; set; }
    /// <summary>
    /// Error logs for unhandled exceptions.
    /// </summary>
    public DbSet<ErrorLog> ErrorLog { get; set; }

    /* =========
     * Admin
     * ========= */
    /// <summary>
    /// Global configuration settings for the application.
    /// </summary>
    public DbSet<AppConfig> AppConfig { get; set; }

    /* =========
     * User
     * ========= */
    // Users and Roles provided by Identity
    /// <summary>
    /// User's personal information.
    /// </summary>
    public DbSet<Employee> Employee { get; set; }
    /// <summary>
    /// Worker's IP addresses.
    /// </summary>
    public DbSet<WorkerIps> WorkerIps { get; set; }
    /// <summary>
    /// User's contact information.
    /// </summary>
    public DbSet<PostalCode> PostalCode { get; set; }
    /// <summary>
    /// User's school information.
    /// </summary>
    public DbSet<School> School { get; set; }
    /// <summary>
    /// User's student information.
    /// </summary>
    public DbSet<Student> Student { get; set; }
    /// <summary>
    /// User's category.
    /// </summary>
    public DbSet<UserCategory> UserCategory { get; set; }

    /* =========
     * Inventory
     * ========= */
    /// <summary>
    /// Products.
    /// </summary>
    public DbSet<Product> Product { get; set; }
    /// <summary>
    /// Product categories.
    /// </summary>
    public DbSet<ProductCategory> ProductCategory { get; set; }

    /* =========
     * Order
     * ========= */
    /// <summary>
    /// Orders.
    /// </summary>
    public DbSet<Order> Order { get; set; }
    /// <summary>
    /// Order lines.
    /// </summary>
    public DbSet<OrderLine> OrderLine { get; set; }
    /// <summary>
    /// Balance orders for users, representing pre-paid credits that can be used for purchases.
    /// </summary>
    public DbSet<BalanceOrder> BalanceOrder { get; set; }
    /// <summary>
    /// Discount codes for promotions and discounts.
    /// </summary>
    public DbSet<Discount> Discount { get; set; }

    /* =========
     * Payment
     * ========= */
    /// <summary>
    /// Payment methods and their details.
    /// </summary>
    public DbSet<Transaction> Transaction { get; set; }

    /* =========
     * Tickets
     * ========= */
    /// <summary>
    /// Tickets.
    /// </summary>
    public DbSet<Ticket> Ticket { get; set; }
    /// <summary>
    /// Ticket prices.
    /// </summary>
    public DbSet<TicketPrice> TicketPrice { get; set; }
    /// <summary>
    /// Records of ticket purchases.
    /// </summary>
    public DbSet<TicketPurchase> TicketPurchase { get; set; }
    /// <summary>
    /// Records of ticket transfers between users.
    /// </summary>
    public DbSet<TicketTransfer> TicketTransfer { get; set; }

    /* ==========
     * Fluent API
     * ========== */
    /// <summary>
    /// Fluent API for defining relationships and constraints.
    /// </summary>
    /// <param name="modelBuilder">The ModelBuilder used to configure the entity relationships and constraints.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set up Identity tables
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.UserCategory)
            .WithMany()
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserCategory>()
            .HasMany(uc => uc.TicketPrices)
            .WithOne(tp => tp.UserCategory)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.RedemptionCode)
            .IsUnique();

        // Composite Key for OrderLine
        modelBuilder.Entity<OrderLine>()
            .HasKey(ol => new { ol.ProductId, ol.OrderId });

        modelBuilder.Entity<OrderLine>()
            .HasOne(ol => ol.Product)
            .WithMany(p => p.ProductPurchases)
            .HasForeignKey(ol => ol.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderLine>()
            .HasOne(ol => ol.Order)
            .WithMany(o => o.ProductPurchases)
            .HasForeignKey(ol => ol.OrderId);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Owner)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict); // Prevent multiple cascade
            
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.ValidationCode)
            .IsUnique();

        modelBuilder.Entity<TicketTransfer>()
            .HasOne(tt => tt.Sender)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketTransfer>()
            .HasOne(tt => tt.Receiver)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<TicketPrice>()
            .HasOne(tp => tp.UserCategory)
            .WithMany(uc => uc.TicketPrices)
            .OnDelete(DeleteBehavior.Restrict);

        // Global configuration for Decimals
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // TPT (Table Per Type) Inheritance Mapping
        modelBuilder.Entity<AppUser>().ToTable("User");
        modelBuilder.Entity<Student>().ToTable("Student");
        modelBuilder.Entity<Employee>().ToTable("Employee");
        modelBuilder.Entity<WorkerIps>().ToTable("WorkerIps");
    }
}