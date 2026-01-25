using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Necessário para o Identity
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Models;

namespace Projeto_SEGUES.Data
{
    // ALTERAÇÃO 1: Herdar de IdentityDbContext<User> para ligar a autenticação
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // =================================================================
        // 1. ÁREA DE UTILIZADORES
        // =================================================================
        // O DbSet<User> já existe dentro do IdentityDbContext, mas pode manter se quiser
        public DbSet<User> Users { get; set; } 

        public DbSet<Student> Students { get; set; }
        public DbSet<AdministratorEmployee> AdministratorEmployees { get; set; }
        public DbSet<External> External { get; set; }

        public DbSet<PostalCode> PostalCodes { get; set; }
        public DbSet<School> Schools { get; set; }

        // =================================================================
        // 2. ÁREA DE PRODUTOS, COMPRAS E DESCONTOS
        // =================================================================
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<ProductPurchase> ProductPurchases { get; set; }
        public DbSet<Discount> Discounts { get; set; }

        // =================================================================
        // 3. ÁREA DE BILHETES (TICKETS)
        // =================================================================
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketPurchase> TicketPurchases { get; set; }
        public DbSet<TicketTransfer> TicketTransfers { get; set; }
        public DbSet<TicketPrice> TicketPrices { get; set; }

        // =================================================================
        // 4. FINANÇAS E LOGS
        // =================================================================
        public DbSet<BalanceCharge> BalanceCharges { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<LogError> LogErrors { get; set; }
        public DbSet<AlertSignalLog> AlertSignalLogs { get; set; }
        public DbSet<DbStats> DbStats { get; set; }

        // =================================================================
        // CONFIGURAÇÕES AVANÇADAS (FLUENT API)
        // =================================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // OBRIGATÓRIO: Configura as tabelas do Identity (AspNetUsers, etc.)
            base.OnModelCreating(modelBuilder);

            // --- A. Configuração da Tabela Intermédia (Products_Purchase) ---
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


            // --- CORREÇÃO DO ERRO (Multiple Cascade Paths) ---
            // O erro acontecia porque ao apagar um User, o SQL tentava apagar o Ticket por dois caminhos.
            // Aqui dizemos: Se apagar o User, NÃO apague o Ticket automaticamente via OwnerId.
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Owner)       // Relação com o User
                .WithMany()                 // Se tiver lista de tickets no User, ponha .WithMany(u => u.Tickets)
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict); // <--- MUDANÇA IMPORTANTE (Era Cascade por padrão)


            // --- B. Configuração TicketTransfer (Evitar Ciclos de Delete) ---
            modelBuilder.Entity<TicketTransfer>()
                .HasOne(tt => tt.Sender)
                .WithMany()
                .HasForeignKey(tt => tt.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketTransfer>()
                .HasOne(tt => tt.Receiver)
                .WithMany()
                .HasForeignKey(tt => tt.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- C. Configuração Global para Decimais (Dinheiro) ---
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }

            // --- D. Configurações Específicas ---
            modelBuilder.Entity<Discount>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Mapeamento TPT
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<AdministratorEmployee>().ToTable("Administrator_Employee");
            modelBuilder.Entity<External>().ToTable("External");
        }
    }
}