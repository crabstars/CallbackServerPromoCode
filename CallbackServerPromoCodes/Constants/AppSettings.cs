namespace CallbackServerPromoCodes.Constants;

public static class AppSettings
{
    public const string Serilog = "Logging:Path:Serilog";

    public const string HmacSecret = "Secrets:HmacPubSubHub";

    public const string DbConnection = "ConnectionStrings:Sqlite";

    public const string CallbackApiKey = "Secrets:CallbackApiKey";

    public const string YoutubeApiKey = "Secrets:YoutubeApiKey";

    public const string OpenAiApiKey = "Secrets:OpenAIApiKey";

    public const string ProcessVideoDelay = "WorkerDelay:ProcessVideo";

    public const string CallbackBaseUrl = "Hub:Callback";
}