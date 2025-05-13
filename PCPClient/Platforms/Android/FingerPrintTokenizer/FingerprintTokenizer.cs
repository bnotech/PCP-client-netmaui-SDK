using Android.Content;
using Android.Util;
using Android.Webkit;
using Java.Util;
using Com.Payone.PcpClientSdk.Android.Utils;
using AWebView = Android.Webkit.WebView;
using Object = Java.Lang.Object;

namespace Com.Payone.PcpClientSdk.Android.FingerprintTokenizer;

public class FingerprintTokenizer
{
    readonly Context context;
    readonly string paylaPartnerId;
    readonly string partnerMerchantId;
    readonly PCPEnvironment environment;
    readonly string snippetToken;
    AWebView webView;
    Action<Com.Payone.PcpClientSdk.Android.Result<string>> onCompletion;

    public FingerprintTokenizer(
        Context ctx,
        string paylaPartnerId,
        string partnerMerchantId,
        PCPEnvironment environment,
        string sessionId = null)
    {
        this.context = ctx;
        this.paylaPartnerId = paylaPartnerId;
        this.partnerMerchantId = partnerMerchantId;
        this.environment = environment;
        var uniqueId = sessionId ?? UUID.RandomUUID().ToString();
        snippetToken = $"{paylaPartnerId}_{partnerMerchantId}_{uniqueId}";
    }

    public void GetSnippetToken(Action<Com.Payone.PcpClientSdk.Android.Result<string>> onCompletion)
    {
        this.onCompletion = onCompletion;
        string script = MakeScript();
        webView = MakeInjectedWebView(script);
        webView.LoadData(MakeHtml(), "text/html", "UTF-8");
    }

    private string MakeHtml() =>
        "<!doctype html><html lang=\"en\"><body></body></html>";

    private string MakeScript() => $@"
window.paylaDcs = window.paylaDcs || {{}};
var script = document.createElement('script');
script.id = 'paylaDcs';
script.type = 'text/javascript';
script.src = 'https://d.payla.io/dcs/{paylaPartnerId}/{partnerMerchantId}/dcs.js';
script.onload = function() {{
    if (window.paylaDcs && window.paylaDcs.init) {{
        window.paylaDcs.init('{environment.FingerprintTokenizerIdentifier}', '{snippetToken}');
    }} else {{
        throw new Error('paylaDcs not defined or missing init');
    }}
}};
document.body.appendChild(script);
";

    private AWebView MakeInjectedWebView(string script)
    {
        var wv = new AWebView(context);
        wv.Settings.JavaScriptEnabled = true;
        wv.Settings.DomStorageEnabled = true;

        wv.SetWebViewClient(new InternalClient(this, script));
        return wv;
    }

    class InternalClient : WebViewClient
    {
        readonly FingerprintTokenizer parent;
        readonly string script;

        public InternalClient(FingerprintTokenizer p, string script)
        {
            parent = p;
            this.script = script;
        }

        public override void OnPageFinished(AWebView view, string url)
        {
            view.EvaluateJavascript(script, new OnPageFinishedValueCallback(parent));
        }

        public override bool ShouldOverrideUrlLoading(AWebView view, IWebResourceRequest request)
            => false;

        class OnPageFinishedValueCallback(FingerprintTokenizer parent) : Java.Lang.Object, IValueCallback
        {
            public void OnReceiveValue(Object? value)
            {
                try
                {
                    Log.Info("Fingerprint", "Successfully loaded snippet token.");
                    parent.onCompletion(
                        Result<string>.Success(parent.snippetToken));
                }
                catch (Exception e)
                {
                    Log.Error("Fingerprint", $"Script failed: {e}");
                    parent.onCompletion(
                        Result<string>.Failure(
                            new FingerprintError.ScriptError(e)));
                }
            }
        }
    }
}