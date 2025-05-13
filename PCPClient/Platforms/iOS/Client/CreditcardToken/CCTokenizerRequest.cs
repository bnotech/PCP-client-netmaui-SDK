using System.Security.Cryptography;
using System.Text;

namespace Com.Payone.PcpClientSdk.iOS;

public class CCTokenizerRequest
{
    public string Mid       { get; }
    public string Aid       { get; }
    public string PortalId  { get; }
    public PCPEnvironment Environment { get; }
    public string GeneratedHash { get; }

    public CCTokenizerRequest(
        string mid,
        string aid,
        string portalId,
        PCPEnvironment environment,
        string pmiPortalKey
    )
    {
        Mid         = mid;
        Aid         = aid;
        PortalId    = portalId;
        Environment = environment;

        GeneratedHash = MakeHash(environment, mid, aid, portalId, pmiPortalKey);
    }

    static string MakeHash(
        PCPEnvironment environment,
        string mid,
        string aid,
        string portalId,
        string secret
    )
    {
        // request values:
        var list = new[]
        {
            aid, "3.11", "UTF-8", mid,
            environment.CcTokenizerIdentifier(),
            portalId, "creditcardcheck", "JSON", "yes"
        };
        var sign = string.Concat(list) + secret;
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(sign));
        return Convert.ToBase64String(hashBytes);
    }
}