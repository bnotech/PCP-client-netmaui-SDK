using ObjCRuntime;

namespace Com.Payone.PcpClientSdk.iOS;

[Native]
public enum PayoneLanguage : long
{
    English,
    German
}

public static class PayoneLanguageExtensions
{
    internal static string ConfigValue(this PayoneLanguage l) =>
        l == PayoneLanguage.English
            ? "Payone.ClientApi.Language.en"
            : "Payone.ClientApi.Language.de";
}
