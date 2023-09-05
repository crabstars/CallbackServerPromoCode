using System.Text.Json.Serialization;

namespace CallbackServerPromoCodes.ApiModels;

public class ChatGptResponse
{
    [JsonPropertyName("choices")] public List<Choice> Choices { get; set; }

    public sealed class Choice
    {
        [JsonPropertyName("message")] public ChoiceMessage Message { get; set; }

        public sealed class ChoiceMessage
        {
            [JsonPropertyName("content")] public string Content { get; set; }
        }
    }
}