using System;

namespace Com.Payone.PcpClientSdk.Android.FingerprintTokenizer
{
    public abstract class FingerprintError : Exception
    {
        protected FingerprintError(string message, Exception inner = null)
            : base(message, inner) {}

        public sealed class ScriptError : FingerprintError
        {
            public ScriptError(Exception inner)
                : base("Script error", inner) {}
        }

        public sealed class Undefined : FingerprintError
        {
            private Undefined() : base("Undefined") {}
            public static readonly Undefined Instance = new Undefined();
        }
    }
}