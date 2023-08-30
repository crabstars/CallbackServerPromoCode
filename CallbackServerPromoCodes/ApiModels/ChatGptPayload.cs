using System.Text.Json.Serialization;

namespace CallbackServerPromoCodes.ApiModels;

public class ChatGptPayload
{
    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("messages")] public List<ChatGptMessage> Messages { get; set; }
}