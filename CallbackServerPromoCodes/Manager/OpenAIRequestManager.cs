using System.Text;
using System.Text.Json;
using CallbackServerPromoCodes.ApiModels;

namespace CallbackServerPromoCodes.Manager;

public static class OpenAIRequestManager
{
    private const string OpenAiApiBase = "https://api.openai.com/v1/";

    public static async Task<List<ExtractedGptPromo>> GetPromotions(HttpClient httpClient, ILogger logger,
        string apiKey, string description, CancellationToken cancellationToken)
    {
        const string url = OpenAiApiBase + "chat/completions";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var payload = new ChatGptPayload
        {
            Model = "gpt-3.5-turbo",
            Messages = new List<ChatGptMessage>
            {
                new()
                {
                    Role = "system",
                    Content =
                        "You are an promotion link and promotion code extractor which returns a list of json object with an 'link' and 'code' and 'company' attribute."
                },
                new() { Role = "user", Content = description }
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await httpClient.SendAsync(request, cancellationToken);

        // Check the response status code
        if (!response.IsSuccessStatusCode) return null;
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatGptResponse = JsonSerializer.Deserialize<ChatGptResponse>(responseBody);
        try
        {
            var promo = JsonSerializer.Deserialize<List<ExtractedGptPromo>>(chatGptResponse?.Choices[0].Message
                .Content);
            logger.LogInformation("Extracted list of promotions");
            return promo ?? new List<ExtractedGptPromo>();
        }
        catch (Exception)
        {
            try
            {
                var promo = JsonSerializer.Deserialize<ExtractedGptPromo>(chatGptResponse?.Choices[0].Message.Content);
                logger.LogInformation("Extracted one promotion");
                return promo is not null ? new List<ExtractedGptPromo> { promo } : new List<ExtractedGptPromo>();
            }
            catch (Exception)
            {
                logger.LogWarning(
                    "Could not extract any promotions from description: {desc}\n\nChatGpt Response: {resp}",
                    description, chatGptResponse);
                return new List<ExtractedGptPromo>();
            }
        }
    }
}