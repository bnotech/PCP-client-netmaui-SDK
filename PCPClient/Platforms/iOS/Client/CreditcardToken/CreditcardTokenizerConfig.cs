namespace Com.Payone.PcpClientSdk.iOS;

public class CreditcardTokenizerConfig
{
    public Field CardPan           { get; }
    public Field CardCvc2          { get; }
    public Field CardExpireMonth   { get; }
    public Field CardExpireYear    { get; }
    public Dictionary<string,string> DefaultStyles { get; }
    public PayoneLanguage Language  { get; }
    public string ErrorMessage      { get; }
    public string SubmitButtonId    { get; }

    internal Action<Result<CCTokenizerResponse,CCTokenizerError>> CreditCardCheckCallback { get; }

    public CreditcardTokenizerConfig(
        Field cardPan,
        Field cardCvc2,
        Field cardExpireMonth,
        Field cardExpireYear,
        Dictionary<string,string> defaultStyles,
        PayoneLanguage language,
        string errorMessage,
        string submitButtonId,
        Action<Result<CCTokenizerResponse,CCTokenizerError>> creditCardCheckCallback
    )
    {
        CardPan     = cardPan;
        CardCvc2    = cardCvc2;
        CardExpireMonth = cardExpireMonth;
        CardExpireYear  = cardExpireYear;
        DefaultStyles   = defaultStyles ?? new Dictionary<string,string>();
        Language        = language;
        ErrorMessage    = errorMessage;
        SubmitButtonId  = submitButtonId;
        CreditCardCheckCallback = creditCardCheckCallback;
    }
}