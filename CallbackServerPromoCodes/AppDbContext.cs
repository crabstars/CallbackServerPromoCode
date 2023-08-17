using CallbackServerPromoCodes.Models;
using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes;

public class AppDbContext : DbContext
{
    public DbSet<Video> Videos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dataSource = "Data Source=/mnt/PromoCodes.db";
        // TODO change to configuration
#if DEBUG
        dataSource = "Data Source=DB/PromoCodes.db";
#endif
        optionsBuilder.UseSqlite(dataSource);
    }
}