using Fastfood.Models;
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
        //public DbSet<Users> users { get; set; }
    }
}
