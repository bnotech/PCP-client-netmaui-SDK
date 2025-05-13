using Foundation;

namespace Com.Payone.PcpClientSdk.iOS;

public static class NSDictionaryExtensions
{
    public static Dictionary<string, string> ToDictionary(this NSDictionary ns)
    {
        var d = new Dictionary<string, string>();
        foreach (var key in ns.Keys)
        {
            d[key.ToString()] = ns.ObjectForKey(key).ToString();
        }

        return d;
    }
}
