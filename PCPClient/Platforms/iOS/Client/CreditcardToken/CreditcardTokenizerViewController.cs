using System.Text;
using System.Text.Json;
using Foundation;
using UIKit;
using WebKit;

namespace Com.Payone.PcpClientSdk.iOS;

public class CreditcardTokenizerViewController : UIViewController, IWKNavigationDelegate, IWKScriptMessageHandler
{
    readonly NSUrl tokenizerUrl;
    readonly string[] supportedCardTypes;
    readonly CreditcardTokenizerConfig config;
    readonly CCTokenizerRequest request;
    WKWebView webView;

    public CreditcardTokenizerViewController(
        NSUrl tokenizerUrl,
        CCTokenizerRequest request,
        IEnumerable<string> supportedCardTypes,
        CreditcardTokenizerConfig config
    )
    {
        this.tokenizerUrl = tokenizerUrl;
        this.request = request;
        this.supportedCardTypes = supportedCardTypes.ToArray();
        this.config = config;
    }

    public override void ViewWillAppear(bool animated)
    {
        base.ViewWillAppear(animated);
        SetupWebView();
    }

    void SetupWebView()
    {
        webView = webView ?? new WKWebView(View.Bounds, new WKWebViewConfiguration());
        webView.NavigationDelegate = this;
        View.AddSubview(webView);
        webView.TranslatesAutoresizingMaskIntoConstraints = false;
        NSLayoutConstraint.ActivateConstraints(new[]
        {
            webView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
            webView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
            webView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
            webView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
        });
        webView.LoadRequest(new NSUrlRequest(tokenizerUrl));
    }

    void Initialize()
    {
        CheckRequiredElements(isSetUpCorrectly =>
        {
            if (!isSetUpCorrectly)
            {
                PCPLogger.Error("Not all required elements are available.");
                return;
            }

            var script = MakeScriptToLoadPayoneHostedScript();
            var userScript = new WKUserScript(new NSString(script), WKUserScriptInjectionTime.AtDocumentEnd, true);
            webView.Configuration.UserContentController.AddUserScript(userScript);

            AddScriptMessageHandler(CCScriptMessageType.ScriptLoaded.RawValue());
            AddScriptMessageHandler(CCScriptMessageType.ScriptError.RawValue());

            webView.EvaluateJavaScript(script, (res, err) =>
            {
                if (err != null)
                    PCPLogger.Error("Error injecting load script: " + err.LocalizedDescription);
            });
        });
    }

    string GenerateKeyValuePairs(Field f)
    {
        var sb = new StringBuilder();
        sb.Append($"selector: \"{f.Selector}\", type: \"{f.Type}\"");
        if (!string.IsNullOrEmpty(f.Style))
            sb.Append($", style: \"{f.Style}\"");
        if (!string.IsNullOrEmpty(f.Size))
            sb.Append($", size: \"{f.Size}\"");
        if (!string.IsNullOrEmpty(f.MaxLength))
            sb.Append($", maxlength: \"{f.MaxLength}\"");
        if (f.Length.Any())
        {
            var len = string.Join(", ",
                f.Length.Select(kv => $"{kv.Key}: \"{kv.Value}\""));
            sb.Append($", length: {{ {len} }}");
        }

        if (f.Iframe.Any())
        {
            var ifr = string.Join(", ",
                f.Iframe.Select(kv => $"{kv.Key}: \"{kv.Value}\""));
            sb.Append($", iframe: {{ {ifr} }}");
        }

        return "{ " + sb + " }";
    }

    string GenerateDefaultStyleKeyValuePairs()
    {
        return string.Join(",\n",
            config.DefaultStyles.Select(kv => $"{kv.Key}: \"{kv.Value}\""));
    }

    string MakeScriptToLoadPayoneHostedScript() => @"
            if (!document.getElementById('payone-hosted-script')) {
                const script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = 'https://secure.prelive.pay1-test.de/client-api/js/v1/payone_hosted_min.js';
                script.id = 'payone-hosted-script';
                script.onload = function() {
                    " + CCScriptMessageType.ResponseReceived.MakeWebkitMessageString("\"Script loaded.\"") + @"
                }
                script.onerror = function() {
                    " + CCScriptMessageType.ResponseReceived.MakeWebkitMessageString(
        "\"Failed to load Payone script.\"") + @"
                }
                document.head.appendChild(script);
            }
            null
        ";

    string MakeScriptToPopulateHTML()
    {
        var types = "[" + string.Join(",",
            supportedCardTypes.Select(s => $"\"{s}\"")) + "]";

        return $@"
                var supportedCardtypes = {types};
                var config = {{
                    fields: {{
                        cardpan: {GenerateKeyValuePairs(config.CardPan)},
                        cardcvc2: {GenerateKeyValuePairs(config.CardCvc2)},
                        cardexpiremonth: {GenerateKeyValuePairs(config.CardExpireMonth)},
                        cardexpireyear: {GenerateKeyValuePairs(config.CardExpireYear)}
                    }},
                    defaultStyle: {{
                        {GenerateDefaultStyleKeyValuePairs()}
                    }},
                    autoCardtypeDetection: {{
                        supportedCardtypes: supportedCardtypes,
                        callback: function(detectedCardtype) {{
                            document.getElementById('autodetectionResponsePre').innerHTML = detectedCardtype;
                            if (detectedCardtype === 'V') {{
                                document.getElementById('visa').style.borderColor = '#00F';
                                document.getElementById('mastercard').style.borderColor = '#FFF';
                            }} else if (detectedCardtype === 'M') {{
                                document.getElementById('visa').style.borderColor = '#FFF';
                                document.getElementById('mastercard').style.borderColor = '#00F';
                            }} else {{
                                document.getElementById('visa').style.borderColor = '#FFF';
                                document.getElementById('mastercard').style.borderColor = '#FFF';
                            }}
                        }}
                    }},
                    language: {config.Language.ConfigValue()},
                    error: ""{config.ErrorMessage}""
                }};
                var request = {{
                    request: 'creditcardcheck',
                    responsetype: 'JSON',
                    mode: '{request.Environment.CcTokenizerIdentifier()}',
                    mid: '{request.Mid}',
                    aid: '{request.Aid}',
                    portalid: '{request.PortalId}',
                    encoding: 'UTF-8',
                    storecarddata: 'yes',
                    hash: '{request.GeneratedHash}'
                }};
                document.getElementById('{config.SubmitButtonId}').onclick = function() {{
                    {CCScriptMessageType.ResponseReceived.MakeWebkitMessageString("\"\"")}
                }};
                var iframes = new window.Payone.ClientApi.HostedIFrames(config, request);
                window.payoneIFrames = iframes;
                null
            ";
    }

    string MakeScriptToInitiateAndHandleCheck() => @"
            var iframes = window.payoneIFrames;
            function payCallback(response) {
                " + CCScriptMessageType.ResponseReceived.MakeWebkitMessageString("response") + @"
            }
            iframes.creditCardCheck('payCallback');
        ";

    void CheckRequiredElements(Action<bool> onResult)
    {
        CheckIfElementExists(config.SubmitButtonId, exists => { onResult(exists); });
    }

    void CheckIfElementExists(string elementId, Action<bool> onResult)
    {
        var script = $"document.querySelector('#{elementId}') !== null";
        webView?.EvaluateJavaScript(script, (res, err) =>
        {
            if (err != null)
            {
                onResult(false);
                return;
            }

            if (res is NSNumber num)
            {
                onResult(num.BoolValue);
            }
            else
            {
                onResult(false);
            }
        });
    }

    // MARK: IWKNavigationDelegate
    [Export("webView:didFinishNavigation:")]
    public void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
    {
        Initialize();
    }

    // MARK: IWKScriptMessageHandler
    public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
    {
        var name = message.Name;
        switch (name)
        {
            case var n when n == CCScriptMessageType.ScriptLoaded.RawValue():
                webView.EvaluateJavaScript(MakeScriptToPopulateHTML(), (res, err) =>
                {
                    if (err != null)
                    {
                        PCPLogger.Error("Populating HTML failed: " + err.LocalizedDescription);
                        config.CreditCardCheckCallback(
                            Result<CCTokenizerResponse, CCTokenizerError>.Fail(CCTokenizerError.PopulatingHTMLFailed));
                    }
                });
                AddScriptMessageHandler(CCScriptMessageType.SubmitButtonClicked.RawValue());
                break;

            case var n when n == CCScriptMessageType.ScriptError.RawValue():
                PCPLogger.Error("Loading Payone script failed.");
                config.CreditCardCheckCallback(
                    Result<CCTokenizerResponse, CCTokenizerError>.Fail(CCTokenizerError.LoadingScriptFailed));
                break;

            case var n when n == CCScriptMessageType.SubmitButtonClicked.RawValue():
                webView.EvaluateJavaScript(MakeScriptToInitiateAndHandleCheck(), (result, error) => { });
                AddScriptMessageHandler(CCScriptMessageType.ResponseReceived.RawValue());
                break;

            case var n when n == CCScriptMessageType.ResponseReceived.RawValue():
                if (message.Body is NSDictionary dict)
                {
                    // Convert NSDictionary to JSON then to CCTokenizerResponse
                    var js = JsonSerializer.Serialize(ToDotNet(dict));
                    try
                    {
                        var resp = JsonSerializer.Deserialize<CCTokenizerResponse>(js);
                        config.CreditCardCheckCallback(Result<CCTokenizerResponse, CCTokenizerError>.Success(resp));
                    }
                    catch
                    {
                        PCPLogger.Error("Invalid response received.");
                        config.CreditCardCheckCallback(
                            Result<CCTokenizerResponse, CCTokenizerError>.Fail(CCTokenizerError.InvalidResponse));
                    }
                }
                else
                {
                    PCPLogger.Error("Invalid message body.");
                    config.CreditCardCheckCallback(
                        Result<CCTokenizerResponse, CCTokenizerError>.Fail(CCTokenizerError.InvalidResponse));
                }

                break;

            default:
                PCPLogger.Warning("Unknown message from WebView: " + name);
                break;
        }
    }

    void AddScriptMessageHandler(string key)
    {
        webView?.Configuration.UserContentController.RemoveAllScriptMessageHandlers();
        webView?.Configuration.UserContentController.AddScriptMessageHandler(this, new NSString(key));
    }

    static Dictionary<string, string> ToDotNet(NSDictionary dict)
    {
        var d = new Dictionary<string, string>();
        foreach (var key in dict.Keys)
        {
            var k = key.ToString();
            var v = dict.ObjectForKey(key)?.ToString();
            d[k] = v;
        }

        return d;
    }
}