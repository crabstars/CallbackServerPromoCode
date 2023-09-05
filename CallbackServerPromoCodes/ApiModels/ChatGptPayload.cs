using System.Text.Json.Serialization;

namespace CallbackServerPromoCodes.ApiModels;

public record ChatGptPayload
{
    [JsonPropertyName("model")] public string Model { get; init; }

    [JsonPropertyName("messages")] public List<ChatGptMessage> Messages { get; init; }
}