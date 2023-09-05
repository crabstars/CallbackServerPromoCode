using CallbackServerPromoCodes.Constants;
using ConfigurationProvider = CallbackServerPromoCodes.Provider.ConfigurationProvider;

namespace CallbackServerPromoCodes.Manager;

public static class YoutubeRequestManager
{
    private const string YoutubeApiBase = "https://www.googleapis.com/youtube/v3/";

    public static async Task<string?> GetResponseForYoutubeChannelsCall(string name,
        HttpClient httpClient, ILogger logger,
        CancellationToken cancellationToken)
    {
        var apiKey = ConfigurationProvider.GetConfiguration().GetSection(AppSettings.YoutubeApiKey).Value ??
                     throw new ArgumentException("Missing YoutubeApiKey in appsettings.json");

        var apiUrl = $"{YoutubeApiBase}search?key={apiKey}&q={name}&type=channel&part=snippet";
        return await YoutubeGetCall(httpClient, logger, cancellationToken, apiUrl);
    }

    public static async Task<string?> GetResponseForYoutubeVideoCall(string apiKey, string videoId,
        HttpClient httpClient, ILogger logger,
        CancellationToken cancellationToken)
    {
        var apiUrl = $"{YoutubeApiBase}videos?key={apiKey}&id={videoId}&part=snippet";
        return await YoutubeGetCall(httpClient, logger, cancellationToken, apiUrl);
    }

    private static async Task<string?> YoutubeGetCall(HttpClient httpClient, ILogger logger,
        CancellationToken cancellationToken,
        string apiUrl)
    {
        try
        {
            var response = await httpClient.GetAsync(apiUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogError(
                "Error status code occured while communicating with youtube api: {statusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Exception occured while communicating with youtube api: {message}", ex.Message);
            return null;
        }
    }
}