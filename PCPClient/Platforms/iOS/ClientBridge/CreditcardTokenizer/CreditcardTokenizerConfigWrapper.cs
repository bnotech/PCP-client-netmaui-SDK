using Foundation;

namespace Com.Payone.PcpClientSdk.iOS;

[Foundation.Register("CreditcardTokenizerConfigWrapper")]
public class CreditcardTokenizerConfigWrapper : NSObject
{
    public CreditcardTokenizerConfig TokenizerConfig { get; }

    public CreditcardTokenizerConfigWrapper(
        Field cardPan,
        Field cardCvc2,
        Field cardExpireMonth,
        Field cardExpireYear,
        NSDictionary defaultStyles,
        PayoneLanguage language,
        string error,
        string submitButtonId,
        Action<CCTokenizerResponse> success,
        Action<CCTokenizerError> failure
    )
    {
        var ds = defaultStyles?.ToDictionary() ?? new Dictionary<string,string>();
        TokenizerConfig = new CreditcardTokenizerConfig(
            cardPan, cardCvc2, cardExpireMonth, cardExpireYear,
            ds, language, error, submitButtonId,
            result =>
            {
                if (result.IsSuccess)
                    success(result.Value);
                else
                    failure(result.Error);
            });
    }
}
