using CallbackServerPromoCodes.Models;
using Microsoft.EntityFrameworkCore;
using ConfigurationProvider = CallbackServerPromoCodes.Provider.ConfigurationProvider;

namespace CallbackServerPromoCodes;

public class AppDbContext : DbContext
{
    private const string ConnectionPath = "ConnectionStrings:Sqlite";
    public DbSet<Video> Videos { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = ConfigurationProvider.GetConfiguration();
        var dataSource = $"Data Source={configuration.GetSection(ConnectionPath).Value ?? "DB/PromoCodes.db"}";
        optionsBuilder.UseSqlite(dataSource);
    }
}