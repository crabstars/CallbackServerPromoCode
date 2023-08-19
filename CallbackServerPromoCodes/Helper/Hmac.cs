using System.Security.Cryptography;
using System.Text;

namespace CallbackServerPromoCodes.Helper;

public class Hmac
{
    bool VerifyHmac(string message, string receivedHmac, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        
        using var hmac = new HMACSHA256(keyBytes);
        var calculatedHmacBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var calculatedHmac = BitConverter.ToString(calculatedHmacBytes).Replace("-", "").ToLower();

        return string.Equals(calculatedHmac, receivedHmac, StringComparison.OrdinalIgnoreCase);
    }
}