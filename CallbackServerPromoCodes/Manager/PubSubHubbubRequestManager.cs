using System.Text;
using System.Text.Json;
using CallbackServerPromoCodes.ApiModels;
using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.Enums;
using ConfigurationProvider = CallbackServerPromoCodes.Provider.ConfigurationProvider;

namespace CallbackServerPromoCodes.Manager;

public static class PubSubHubbubRequestManager
{
    private const string PubSubBase = "https://pubsubhubbub.appspot.com";

    public static async Task GetSubscriptionDetails(HttpClient httpClient, HttpContext context, string channelId)
    {
        var configProvider = ConfigurationProvider.GetConfiguration();
        var hmacSecret = configProvider.GetSection(AppSettings.HmacSecret).Value ??
                         throw new ArgumentException("Missing secret for HmacPubSubHub in appsettings.json");
        var callBackUrl = (configProvider.GetSection(AppSettings.CallbackBaseUrl).Value ??
                           throw new ArgumentException("Missing value for CallbackBaseUrl in appsettings.json"))
                          + URLPath.Callback;
        var apiUrl =
            $"{PubSubBase}/subscription-details" +
            $"?hub.callback={callBackUrl}" +
            $"&hub.topic=https://www.youtube.com/xml/feeds/videos.xml?channel_id={channelId}" +
            $"&hub.secret={hmacSecret}";
        try
        {
            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                await context.Response.WriteAsync(responseBody);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(response.IsSuccessStatusCode.ToString());
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Request Error: {ex.Message}");
        }
    }

    public static async Task ChangeSubscription(HttpClient httpClient, ILogger logger, HubMode hubMode,
        string channelId)
    {
        var configProvider = ConfigurationProvider.GetConfiguration();
        var hmacSecret = configProvider.GetSection(AppSettings.HmacSecret).Value ??
                         throw new ArgumentException("Missing secret for HmacPubSubHub in appsettings.json");
        var callBackUrl = (configProvider.GetSection(AppSettings.CallbackBaseUrl).Value ??
                           throw new ArgumentException("Missing value for CallbackBaseUrl in appsettings.json"))
                          + URLPath.Callback;
        const string apiUrl = $"{PubSubBase}/subscribe";
        var content = new PubSubHubSubscribePayload
        {
            Callback = callBackUrl,
            Mode = hubMode.ToString(),
            Secret = hmacSecret,
            Verify = "sync",
            Topic = $"https://www.youtube.com/xml/feeds/videos.xml?channel_id={channelId}",
            LeaseNumbers = "",
            VerifyToken = ""
        };

        var payload = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(apiUrl, payload);
        Console.WriteLine(payload.ToString());
        if (response.IsSuccessStatusCode)
            logger.LogInformation("Subscription for channelId: {id} was changed to: {subType}",
                channelId, hubMode.ToString());

        logger.LogError("Subscription type could not be changed. Response: {}", response.Content.ToString());
    }
}