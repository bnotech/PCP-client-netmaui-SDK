using ObjCRuntime;

namespace Com.Payone.PcpClientSdk.iOS;

[Native]
public enum PCPEnvironment : long
{
    Test,
    Production
}

public static class PCPEnvironmentExtensions
{
    internal static string CcTokenizerIdentifier(this PCPEnvironment e) =>
        e == PCPEnvironment.Test ? "test" : "prod";

    internal static string FingerprintTokenizerIdentifier(this PCPEnvironment e) =>
        e == PCPEnvironment.Test ? "t" : "p";
}