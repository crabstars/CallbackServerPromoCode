namespace CallbackServerPromoCodes.Constants;

public static class Auth
{
    public const string PubSubHubSig = "X-Hub-Signature";

    public const string ApiKeyHeader = "x-api-key";

    public const string HubChallenge = "hub.challenge";

    // ReSharper disable once InconsistentNaming
    public const string CallBackURL = "https://promo-codes.duckdns.org/" + URLPath.Callback;
}