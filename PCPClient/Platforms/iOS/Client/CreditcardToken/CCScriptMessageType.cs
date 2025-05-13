namespace Com.Payone.PcpClientSdk.iOS;

internal enum CCScriptMessageType
{
    ScriptLoaded,
    ScriptError,
    SubmitButtonClicked,
    ResponseReceived
}

internal static class CCScriptMessageTypeExtensions
{
    public static string RawValue(this CCScriptMessageType t) => t switch
    {
        CCScriptMessageType.ScriptLoaded           => "scriptLoaded",
        CCScriptMessageType.ScriptError            => "scriptError",
        CCScriptMessageType.SubmitButtonClicked    => "submitButtonClicked",
        CCScriptMessageType.ResponseReceived       => "responseReceived",
        _                                            => ""
    };

    public static string MakeWebkitMessageString(this CCScriptMessageType t, string body)
    {
        var name = t.RawValue();
        return $"window.webkit.messageHandlers.{name}.postMessage({body});";
    }
}