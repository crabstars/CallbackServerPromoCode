using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.Manager;
using CallbackServerPromoCodes.Models;
using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Worker;

public class ProcessVideo : BackgroundService
{
    private readonly ILogger _logger;
    private readonly string _openAiApiKey;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _ytApiKey;

    public ProcessVideo(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _ytApiKey = configuration.GetSection(AppSettings.YoutubeApiKey).Value ??
                    throw new ArgumentException("Missing YoutubeApiKey in appsettings.json");
        _openAiApiKey = configuration.GetSection(AppSettings.OpenAiApiKey).Value ??
                        throw new ArgumentException("Missing OpenAIApiKey in appsettings.json");
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var videos = await dbContext.Videos.Where(v => !v.Processed).ToListAsync(cancellationToken);

        if (videos.Any())
            _logger.LogInformation("Processing {count} videos", videos.Count);


        var httpClient = httpClientFactory.CreateClient();
        foreach (var video in videos)
        {
            if (video.Description is null)
            {
                await SetVideoDescription(video, _ytApiKey, httpClient, _logger, cancellationToken);
                await SetPromoCodes(video, _openAiApiKey, httpClient, _logger, cancellationToken);
            }

            video.Processed = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }


        // TODO get time from appsettings
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }

    private async Task SetPromoCodes(Video video, string openAIApiKey, HttpClient httpClient, ILogger logger,
        CancellationToken cancellationToken)
    {
    }

    private async Task SetVideoDescription(Video video, string ytApiKet, HttpClient httpClient, ILogger logger,
        CancellationToken cancellationToken)
    {
        var response =
            await YoutubeRequestManager.GetResponseForYoutubeVideoCall(ytApiKet, video.Id, httpClient, logger,
                cancellationToken);
        if (string.IsNullOrEmpty(response))
        {
            logger.LogWarning("Setting video description to empty because youtube response was empty. VideoId {}",
                video.Id);
            return;
        }

        var description = ParseManager.GetVideoDescription(response, logger);
        if (string.IsNullOrEmpty(response))
        {
            logger.LogWarning("Setting video description to empty because parsed description was empty. VideoId {}",
                video.Id);
            return;
        }

        video.Description = description;
    }
}