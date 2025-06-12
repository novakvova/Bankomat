using Microsoft.EntityFrameworkCore;
using MyPrivate.Data.Entitys;

namespace MyPrivate.Data
{
    public class ContextATM : DbContext
    {
        public DbSet<BalanceEntity> Balances { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=ep-quiet-sunset-a8fs2ggz-pooler.eastus2.azure.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_8kphXGT2DNJo");
        }
    }
}
