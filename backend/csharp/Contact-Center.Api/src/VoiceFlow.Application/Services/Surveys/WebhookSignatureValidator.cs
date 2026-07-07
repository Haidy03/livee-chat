using System.Security.Cryptography;
using System.Text;

namespace VoiceFlow.Application.Services.Surveys
{
    public static class WebhookSignatureValidator
    {
        public static bool IsValid(string rawBody, string? signatureHeader, string? secret)
        {
            if (string.IsNullOrEmpty(secret)) return true;
            if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

            var provided = signatureHeader.Trim();
            if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
                provided = provided.Substring("sha256=".Length);

            using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expected = Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(rawBody)));

            var a = Encoding.ASCII.GetBytes(provided.ToLowerInvariant());
            var b = Encoding.ASCII.GetBytes(expected.ToLowerInvariant());
            if (a.Length != b.Length) return false;
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
