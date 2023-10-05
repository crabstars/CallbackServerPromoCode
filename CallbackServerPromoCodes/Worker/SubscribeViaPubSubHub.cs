using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.Enums;
using CallbackServerPromoCodes.Manager;
using CallbackServerPromoCodes.Models;
using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Worker;

public class SubscribeViaPubSubHub : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _workerDelay;


    public SubscribeViaPubSubHub(IServiceProvider serviceProvider, IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger("ProcessVideo");
        _workerDelay = Convert.ToInt32(configuration.GetSection(AppSettings.SubscribeViaPubSubHubDelay).Value ??
                                       throw new ArgumentException(
                                           $"Missing {AppSettings.SubscribeViaPubSubHubDelay} in appsettings.json"));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var channels = await dbContext.Channels.Where(v => !v.Subscribed && v.Activated)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Start SubscribeViaPubSubHub worker");
                if (channels.Any())
                {
                    _logger.LogInformation("Subscribing to {count} channels", channels.Count);

                    var httpClient = httpClientFactory.CreateClient();

                    await SubscribeToChannels(channels, httpClient, dbContext, cancellationToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(_workerDelay), cancellationToken);
        }
    }

    private async Task SubscribeToChannels(List<Channel> channels, HttpClient httpClient,
        DbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (var channel in channels)
            if (await PubSubHubbubRequestManager.ChangeSubscription(httpClient, _logger, HubMode.Subscribe, channel.Id))
            {
                channel.Subscribed = true;
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Subscribed to {name} with id {id}", channel.Name, channel.Id);
            }
            else
            {
                _logger.LogWarning("Subscription failed for {name} with id {id}", channel.Name, channel.Id);
            }
    }
}