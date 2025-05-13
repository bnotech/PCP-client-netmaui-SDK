using Foundation;

namespace Com.Payone.PcpClientSdk.iOS;

[Foundation.Register("FingerprintTokenizerWrapper")]
public class FingerprintTokenizerWrapper : NSObject
{
    readonly FingerprintTokenizer tokenizer;

    public FingerprintTokenizerWrapper(
        string paylaPartnerId,
        string partnerMerchantId,
        PCPEnvironment environment,
        string sessionId = null
    )
    {
        tokenizer = new FingerprintTokenizer(paylaPartnerId, partnerMerchantId, environment, sessionId);
    }

    public void GetSnippetToken(Action<string> success, Action<FingerprintErrorWrapper> failure)
    {
        tokenizer.GetSnippetToken(result =>
        {
            if (result.IsSuccess)
                success(result.Value);
            else
                failure(result.Error.ToWrapped());
        });
    }
}
