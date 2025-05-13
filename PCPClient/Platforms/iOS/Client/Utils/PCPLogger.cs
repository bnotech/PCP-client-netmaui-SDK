namespace Com.Payone.PcpClientSdk.iOS;

//
// MARK: - Logger
//
internal static class PCPLogger
{
    static readonly CoreFoundation.OSLog logger = new("PCPClient", "PCP");

    public static void Info(string message)    => Log(message, CoreFoundation.OSLogLevel.Info);
    public static void Error(string message)   => Log(message, CoreFoundation.OSLogLevel.Error);
    public static void Fault(string message)   => Log(message, CoreFoundation.OSLogLevel.Fault);
    public static void Warning(string message) => Log(message, CoreFoundation.OSLogLevel.Default);

    static void Log(string msg, CoreFoundation.OSLogLevel level)
    {
        var prefix = level switch
        {
            CoreFoundation.OSLogLevel.Debug   => "[🛠️]",
            CoreFoundation.OSLogLevel.Info    => "[ℹ️]",
            CoreFoundation.OSLogLevel.Error   => "[🛑]",
            CoreFoundation.OSLogLevel.Fault   => "[💥]",
            _                 => "[⚠️]"
        };
        logger.Log(level, $"{prefix} {msg}");
    }
}