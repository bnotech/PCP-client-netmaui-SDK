using ObjCRuntime;

namespace Com.Payone.PcpClientSdk.iOS;

public enum FingerprintError
{
    ScriptError,
    Undefined
}

[Native]
public enum FingerprintErrorWrapper : long
{
    ScriptError,
    Undefined
}

public static class FingerprintErrorExtensions
{
    public static FingerprintErrorWrapper ToWrapped(this FingerprintError e) =>
        e == FingerprintError.ScriptError ? FingerprintErrorWrapper.ScriptError
            : FingerprintErrorWrapper.Undefined;
}