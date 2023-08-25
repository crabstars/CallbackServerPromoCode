namespace CallbackServerPromoCodes.Manager;

public static class RequestManager
{
    private const string YoutubeApiBase = "https://www.googleapis.com/youtube/v3/search";

    public static async Task<string?> GetResponseForYoutubeChannelsCall(string apiKey, string name,
        HttpClient httpClient, ILogger logger,
        CancellationToken cancellationToken)
    {
        var apiUrl = $"{YoutubeApiBase}?key={apiKey}&q={name}&type=channel&part=snippet";
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