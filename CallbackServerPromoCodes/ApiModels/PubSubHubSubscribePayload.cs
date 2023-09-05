using System.Text.Json.Serialization;

namespace CallbackServerPromoCodes.ApiModels;

public record PubSubHubSubscribePayload
{
    [JsonPropertyName("hub.callback")] public string Callback { get; init; }

    [JsonPropertyName("hub.topic")] public string Topic { get; init; }

    [JsonPropertyName("hub.verify")] public string Verify { get; init; }

    [JsonPropertyName("hub.mode")] public string Mode { get; init; }

    [JsonPropertyName("hub.verify_token")] public string VerifyToken { get; set; }

    [JsonPropertyName("hub.secret")] public string Secret { get; init; }

    [JsonPropertyName("hub.lease_numbers")]
    public string LeaseNumbers { get; set; }
}