using CallbackServerPromoCodes.Models;
using Newtonsoft.Json.Linq;

namespace CallbackServerPromoCodes.Manager;

public static class ParseManager
{
    public static Channel? GetChannel(string responseBody, ILogger logger)
    {
        var itemsArray = ParseItems(responseBody, logger);

        var channelId = itemsArray?[0]["id"]?["channelId"];
        var channelName = itemsArray?[0]["snippet"]?["channelTitle"];

        logger.LogDebug("Response Body: {body}", responseBody);
        return channelId == null || channelName == null
            ? null
            : new Channel(channelId.ToString(), channelName.ToString());
    }

    public static string? GetVideoDescription(string responseBody, ILogger logger)
    {
        var itemsArray = ParseItems(responseBody, logger);
        var description = itemsArray?[0]["snippet"]?["description"];
        logger.LogDebug("Response Body: {body}", responseBody);
        return description?.ToString();
    }

    private static JArray? ParseItems(string responseBody, ILogger logger)
    {
        var responseJson = JObject.Parse(responseBody);
        var itemsArray = (JArray?)responseJson["items"];

        if (itemsArray != null && itemsArray.Count != 0) return itemsArray;

        logger.LogError("Parsed object is empty or null");
        return null;
    }
}