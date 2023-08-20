using System.Security.Cryptography;
using System.Text;
using ConfigurationProvider = CallbackServerPromoCodes.Provider.ConfigurationProvider;

namespace CallbackServerPromoCodes.Helper;

public static class Hmac
{
    private const string Sha1Prefix = "sha1=";

    private const string SecretPath = "Secrets:HmacPubSubHub";

    private static readonly HMACSHA1 HmacSha1;

    static Hmac()
    {
        var configuration = ConfigurationProvider.GetConfiguration();
        var secret = configuration.GetSection(SecretPath).Value;

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException($"Secret key not found in appsettings.json for path: {SecretPath}");

        var secretBytes = Encoding.UTF8.GetBytes(secret);

        // Create the HMACSHA1 instance once and reuse it
        HmacSha1 = new HMACSHA1(secretBytes);
    }

    public static bool Verify(ILogger logger, string payload, string? signatureWithPrefix)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            logger.LogError("payload is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(signatureWithPrefix))
        {
            logger.LogError("signature is empty");
            return false;
        }

        if (!signatureWithPrefix.StartsWith(Sha1Prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var signature = signatureWithPrefix[Sha1Prefix.Length..];
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        var hash = HmacSha1.ComputeHash(payloadBytes);
        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return hashString.Equals(signature);
    }
}