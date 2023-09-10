using System.Text.Json.Serialization;

namespace CallbackServerPromoCodes.ApiModels;

public class ExtractedGptPromo
{
    [JsonPropertyName("code")] public string Code { get; set; }

    [JsonPropertyName("link")] public string Link { get; set; }

    [JsonPropertyName("product")] public string Product { get; set; }
}