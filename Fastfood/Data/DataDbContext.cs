using Fastfood.Models;
using FastFood.Models;
using Microsoft.EntityFrameworkCore;

namespace Fastfood.Data
{
    public class DataDbContext : DbContext
    {
        public DataDbContext(DbContextOptions<DataDbContext> options) : base(options)
        {

        }

        
        public DbSet<Category> categories { get; set; }
        public DbSet<Item> items { get; set; }
        public DbSet<Sales> sales { get; set; }
        public DbSet<SoldItems> soldItems { get; set; }
        public DbSet<BankSattlement> bankSattlements { get; set; }
        public DbSet<Client> clients { get; set; }
        public DbSet<Register> logins { get; set; }
        public DbSet<Method> methods { get; set; }
        public DbSet<UserPermissions> userPermissions { get; set; }
        public DbSet<Suppliers> suppliers { get; set; }
        public DbSet<Inv_Purchase> Inv_Purchases { get; set; }
        public DbSet<Inv_PurchasedItems> Inv_PurchasedItems { get; set; }
        public DbSet<StockTracking> StockTracking { get; set; }
        public DbSet<UnitPrice> UnitPrices { get; set; }
        public DbSet<RawMaterial_Items_Consumption> RawMaterial_Items_Consumption { get; set; }

        public DbSet<Consumeable> Consumeables { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StockTracking>()
                .HasOne(st => st.UnitPrice)
                .WithMany(u => u.StockTrackings)
                .HasForeignKey(st => st.UnitId)
                .OnDelete(DeleteBehavior.Restrict); // or .SetNull / Cascade as you prefer
        }

    }
}
