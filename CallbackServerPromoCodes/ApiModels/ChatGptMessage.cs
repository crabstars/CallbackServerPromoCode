using System.Text.Json.Serialization;

namespace CallbackServerPromoCodes.ApiModels;

public class ChatGptMessage
{
    [JsonPropertyName("role")] public string Role { get; set; }

    [JsonPropertyName("content")] public string Content { get; set; }
}