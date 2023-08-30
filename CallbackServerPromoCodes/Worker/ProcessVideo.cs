using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.Manager;
using CallbackServerPromoCodes.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace CallbackServerPromoCodes.Worker;

public class ProcessVideo : BackgroundService
{
    private readonly ILogger _logger;
    private readonly string _openAiApiKey;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _workerDelay;
    private readonly string _ytApiKey;

    public ProcessVideo(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = Log.Logger;
        _ytApiKey = configuration.GetSection(AppSettings.YoutubeApiKey).Value ??
                    throw new ArgumentException($"Missing {AppSettings.YoutubeApiKey} in appsettings.json");
        _openAiApiKey = configuration.GetSection(AppSettings.OpenAiApiKey).Value ??
                        throw new ArgumentException($"Missing {AppSettings.OpenAiApiKey} in appsettings.json");
        _workerDelay = Convert.ToInt32(configuration.GetSection(AppSettings.ProcessVideoDelay).Value ??
                                       throw new ArgumentException(
                                           $"Missing {AppSettings.ProcessVideoDelay} in appsettings.json"));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var videos = await dbContext.Videos.Where(v => !v.Processed).ToListAsync(cancellationToken);

        if (videos.Any())
            _logger.Information("Processing {count} videos", videos.Count);


        var httpClient = httpClientFactory.CreateClient();
        foreach (var video in videos)
        {
            if (video.Description is null)
            {
                await SetVideoDescription(video, httpClient, _logger, cancellationToken);
                await SetPromoCodes(video, httpClient, _logger, cancellationToken);
            }

            video.Processed = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await Task.Delay(TimeSpan.FromMinutes(_workerDelay), cancellationToken);
    }

    private async Task SetPromoCodes(Video video, HttpClient httpClient, ILogger logger,
        CancellationToken cancellationToken)
    {
        var promos =
            await OpenAiRequestManager.GetPromotions(httpClient, logger, _openAiApiKey, video.Description ?? "");
        video.Promotions = promos.Select(p => new Promotion(p.Code, p.Link, p.Company)).ToList();
    }

    private async Task SetVideoDescription(Video video, HttpClient httpClient, ILogger logger,
        CancellationToken cancellationToken)
    {
        var response =
            await YoutubeRequestManager.GetResponseForYoutubeVideoCall(_ytApiKey, video.Id, httpClient, logger,
                cancellationToken);
        if (string.IsNullOrEmpty(response))
        {
            logger.Warning("Setting video description to empty because youtube response was empty. VideoId {}",
                video.Id);
            return;
        }

        var description = ParseManager.GetVideoDescription(response, logger);
        if (string.IsNullOrEmpty(response))
        {
            logger.Warning("Setting video description to empty because parsed description was empty. VideoId {}",
                video.Id);
            return;
        }

        video.Description = description;
    }
}