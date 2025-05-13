using System;

namespace Com.Payone.PcpClientSdk.Android.CcTokenizer
{
    // Mirrors the sealed Kotlin exceptions as static instances
    public class CCTokenizerError : Exception
    {
        private CCTokenizerError(string message) : base(message) {}

        public static readonly CCTokenizerError LoadingScriptFailed 
            = new CCTokenizerError("Loading script failed.");

        public static readonly CCTokenizerError PopulatingHTMLFailed 
            = new CCTokenizerError("Populating HTML failed.");

        public static readonly CCTokenizerError InvalidResponse
            = new CCTokenizerError("Invalid response.");
    }
}