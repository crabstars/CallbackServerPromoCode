using System.Security.Cryptography;
using System.Text;

namespace CallbackServerPromoCodes.Helper;

public static class Hmac
{
    private const string Sha1Prefix = "sha1=";

    public static bool VerifyHmac(string message, string receivedHmac, string secretKey)
    {
        //testSecret
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        using var hmac = new HMACSHA256(keyBytes);
        var calculatedHmacBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var calculatedHmac = BitConverter.ToString(calculatedHmacBytes).Replace("-", "").ToLower();

        return string.Equals(calculatedHmac, receivedHmac, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Verify(string payload, string signatureWithPrefix)
    {
        if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentNullException(nameof(payload));
        if (string.IsNullOrWhiteSpace(signatureWithPrefix))
            throw new ArgumentNullException(nameof(signatureWithPrefix));

        if (signatureWithPrefix.StartsWith(Sha1Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var signature = signatureWithPrefix.Substring(Sha1Prefix.Length);
            var secret = Encoding.ASCII.GetBytes("testSecret");
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using (var hmSha1 = new HMACSHA1(secret))
            {
                var hash = hmSha1.ComputeHash(payloadBytes);

                var hashString = ToHexString(hash);

                if (hashString.Equals(signature)) return true;
            }
        }

        return false;
    }

    public static string ToHexString(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) builder.AppendFormat("{0:x2}", b);

        return builder.ToString();
    }
}