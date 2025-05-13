using System.Text.Json.Serialization;

namespace Com.Payone.PcpClientSdk.Android.CcTokenizer
{
    public class CCTokenizerResponse
    {
        [JsonPropertyName("cardexpiredate")]
        public string CardExpireDate { get; set; }

        [JsonPropertyName("cardtype")]
        public string CardType { get; set; }

        [JsonPropertyName("errorcode")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("errormessage")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("pseudocardpan")]
        public string PseudoCardpan { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("truncatedcardpan")]
        public string TruncatedCardpan { get; set; }
    }
}