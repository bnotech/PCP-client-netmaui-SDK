using System.Text.Json;
using Android.Runtime;
using Java.Interop;
using Java.IO;

namespace Com.Payone.PcpClientSdk.Android.CcTokenizer
{
    [Serializable]
    public class Field
    {
        public string Selector { get; set; }
        public string Style { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
        public string MaxLength { get; set; }
        public Dictionary<string,int> Length { get; set; }
        public Tuple<string,string> Iframe { get; set; }
    }

    public enum PayoneLanguage
    {
        English, // configValue: "Payone.ClientApi.Language.en"
        German   // configValue: "Payone.ClientApi.Language.de"
    }

    [Serializable]
    public class CreditcardTokenizerConfig : Java.Lang.Object, ISerializable
    {
        public Field CardPan { get; set; }
        public Field CardCvc2 { get; set; }
        public Field CardExpireMonth { get; set; }
        public Field CardExpireYear { get; set; }
        public Dictionary<string,string> DefaultStyles { get; set; }
        public PayoneLanguage Language { get; set; }
        public string Error { get; set; }
        public string SubmitButtonId { get; set; }

        // Callback delivering a Result<CCTokenizerResponse>
        public Action<Result<CCTokenizerResponse>> CreditCardCheckCallback { get; set; }
        
        public CreditcardTokenizerConfig(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {}
        
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
            var result = JsonSerializer.Deserialize<CreditcardTokenizerConfig>(data);
            if (result == null) return;
            CardPan = result.CardPan;
            CardCvc2 = result.CardCvc2;
            CardExpireMonth = result.CardExpireMonth;
            CardExpireYear = result.CardExpireYear;
            DefaultStyles = result.DefaultStyles;
            Language = result.Language;
            Error = result.Error;
            SubmitButtonId = result.SubmitButtonId;
        }
    }
}