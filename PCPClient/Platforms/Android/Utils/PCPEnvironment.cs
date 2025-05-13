namespace Com.Payone.PcpClientSdk.Android.Utils
{
    // Matches Kotlin enum with properties
    public sealed class PCPEnvironment
    {
        public static readonly PCPEnvironment Test = 
            new PCPEnvironment("test", "t");
        public static readonly PCPEnvironment Production = 
            new PCPEnvironment("prod", "p");

        public string CcTokenizerIdentifier { get; }
        public string FingerprintTokenizerIdentifier { get; }

        private PCPEnvironment(string ccId, string fpId)
        {
            CcTokenizerIdentifier = ccId;
            FingerprintTokenizerIdentifier = fpId;
        }
    }
}