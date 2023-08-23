using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.Models;
using Microsoft.EntityFrameworkCore;
using ConfigurationProvider = CallbackServerPromoCodes.Provider.ConfigurationProvider;

namespace CallbackServerPromoCodes;

public class AppDbContext : DbContext
{
    public DbSet<Video> Videos { get; set; }

    public DbSet<Channel> Channels { get; set; }

    public DbSet<Promotion> Promotions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = ConfigurationProvider.GetConfiguration();
        var dataSource = $"Data Source={configuration.GetSection(AppSettings.DbConnection).Value
                                        ?? "DB/PromoCodes.db"}";
        optionsBuilder.UseSqlite(dataSource);
    }
}