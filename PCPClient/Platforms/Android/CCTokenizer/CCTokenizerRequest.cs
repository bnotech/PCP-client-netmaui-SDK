using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Android.Runtime;
using Com.Payone.PcpClientSdk.Android.Utils;
using Java.Interop;
using Java.IO;

namespace Com.Payone.PcpClientSdk.Android.CcTokenizer
{
    [Serializable]
    public class CCTokenizerRequest : Java.Lang.Object, ISerializable
    {
        public string Mid { get; private set; }
        public string Aid { get; private set; }
        public string PortalId { get; private set; }
        public PCPEnvironment Environment { get; private set; }
        public string GeneratedHash { get; private set; }

        private CCTokenizerRequest(string mid, string aid, string portalId, PCPEnvironment env, string hash)
        {
            Mid = mid;
            Aid = aid;
            PortalId = portalId;
            Environment = env;
            GeneratedHash = hash;
        }
        
        public CCTokenizerRequest(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {}

        public static CCTokenizerRequest Create(
            string mid,
            string aid,
            string portalId,
            PCPEnvironment environment,
            string pmiPortalKey)
        {
            var generatedHash = MakeHash(environment, mid, aid, portalId, pmiPortalKey);
            return new CCTokenizerRequest(mid, aid, portalId, environment, generatedHash);
        }

        private static string MakeHash(
            PCPEnvironment environment,
            string mid,
            string aid,
            string portalId,
            string pmiPortalKey)
        {
            // same ordering as Kotlin
            var parts = new[]
            {
                aid,
                "3.11",
                "UTF-8",
                mid,
                environment.CcTokenizerIdentifier,
                portalId,
                "creditcardcheck",
                "JSON",
                "yes"
            };
            var payload = string.Concat(parts);
            return CreateHmacBase64(payload, pmiPortalKey);
        }

        private static string CreateHmacBase64(string data, string secret)
        {
            // HMAC SHA-512 over (data + secret), then Base64 no-wrap
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var textBytes = Encoding.UTF8.GetBytes(data + secret);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hash = hmac.ComputeHash(textBytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        [Export("writeObject", Throws = new[] { typeof(Java.IO.IOException), typeof(Java.Lang.ClassNotFoundException) })]
        private void WriteObject(ObjectOutputStream dest)
        {
            dest.WriteUTF(JsonSerializer.Serialize(this));
        }

        [Export("readObject", Throws = new[] { typeof(Java.IO.IOException), typeof(Java.Lang.ClassNotFoundException) })]
        private void ReadObject(ObjectInputStream source)
        {
            if (source.Available() <= 0) return;
            var data = source.ReadUTF();
            if (data == null) return;
            var result = JsonSerializer.Deserialize<CCTokenizerRequest>(data);
            if (result == null) return;
            Mid = result.Mid;
            Aid = result.Aid;
            PortalId = result.PortalId;
            Environment = result.Environment;
            GeneratedHash = result.GeneratedHash;
        }
    }
}
