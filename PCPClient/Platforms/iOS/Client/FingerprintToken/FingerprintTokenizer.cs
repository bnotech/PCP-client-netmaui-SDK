using Foundation;
using WebKit;

namespace Com.Payone.PcpClientSdk.iOS;

public class FingerprintTokenizer : NSObject, IWKNavigationDelegate
{
    readonly string paylaPartnerId, partnerMerchantId, snippetToken;
    readonly PCPEnvironment environment;
    WKWebView webView;
    Action<Result<string, FingerprintError>> onCompletion;

    public FingerprintTokenizer(string paylaPartnerId,
        string partnerMerchantId,
        PCPEnvironment environment,
        string sessionId = null)
    {
        this.paylaPartnerId = paylaPartnerId;
        this.partnerMerchantId = partnerMerchantId;
        this.environment = environment;
        var uid = sessionId ?? Guid.NewGuid().ToString();
        snippetToken = $"{paylaPartnerId}_{partnerMerchantId}_{uid}";
    }

    public void GetSnippetToken(Action<Result<string, FingerprintError>> onCompletion)
    {
        this.onCompletion = onCompletion;
        var cfg = new WKWebViewConfiguration();
        var userScript = new WKUserScript(new NSString(MakeScript()), WKUserScriptInjectionTime.AtDocumentEnd, true);
        cfg.UserContentController.AddUserScript(userScript);
        webView = new WKWebView(CoreGraphics.CGRect.Empty, cfg)
        {
            NavigationDelegate = this
        };
        webView.LoadHtmlString(MakeHtml(), baseUrl: null);
    }

    string MakeHtml() => @"<!doctype html><html lang=""en""><body></body></html>";

    string MakeScript() => $@"
            window.paylaDcs = window.paylaDcs || {{}};
            var script = document.createElement('script');
            script.id = 'paylaDcs';
            script.type = 'text/javascript';
            script.src = 'https://d.payla.io/dcs/{paylaPartnerId}/{partnerMerchantId}/dcs.js';
            script.onload = function() {{
                if (window.paylaDcs && window.paylaDcs.init)
                    window.paylaDcs.init('{environment.FingerprintTokenizerIdentifier()}', '{snippetToken}');
                else
                    throw new Error('paylaDcs init missing');
            }};
            document.body.appendChild(script);
        ";

    [Export("webView:didFinishNavigation:")]
    public void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
    {
        var invoke = $"paylaDcs.init(\"{environment.FingerprintTokenizerIdentifier()}\", \"{snippetToken}\");";
        webView.EvaluateJavaScript(invoke, (res, err) =>
        {
            if (err != null)
            {
                PCPLogger.Error("Fingerprinting script failed: " + err.LocalizedDescription);
                onCompletion?.Invoke(Result<string, FingerprintError>.Fail(FingerprintError.ScriptError));
            }
            else
            {
                PCPLogger.Info("Successfully loaded snippet token.");
                onCompletion?.Invoke(Result<string, FingerprintError>.Success(snippetToken));
            }
        });
    }
}