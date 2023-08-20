namespace CallbackServerPromoCodes.Provider;

public static class ConfigurationProvider
{
    private const string AppsettingsJson = "appsettings.json";

    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(AppsettingsJson)
            .Build();
    }
}