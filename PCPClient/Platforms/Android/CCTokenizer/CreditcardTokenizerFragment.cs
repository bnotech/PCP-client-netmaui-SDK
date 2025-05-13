using Android.OS;
using Android.Util;
using Android.Views;
using Android.Webkit;
using AWebView = Android.Webkit.WebView;
using AView = Android.Views.View;
using AndroidX.Fragment.App;
using Java.Interop;
using System.Text.Json;
using AResource = Microsoft.Maui.Controls.Resource;

namespace Com.Payone.PcpClientSdk.Android.CcTokenizer
{
    public class CreditcardTokenizerFragment : Fragment
    {
        private AWebView webView;
        private string tokenizerUrl;
        private CCTokenizerRequest request;
        private List<string> supportedCardTypes;
        private CreditcardTokenizerConfig config;
        private Handler handler = new Handler(Looper.MainLooper);

        public override AView OnCreateView(
            LayoutInflater inflater,
            ViewGroup container,
            Bundle savedInstanceState)
        {
            return inflater.Inflate(AResource.Layout.fragment_layout_cctokenizer, container, false);
        }

        public override void OnViewCreated(AView view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            webView = view.FindViewById<AWebView>(AResource.Id.webView);

            var args = Arguments;
            if (args != null)
            {
                tokenizerUrl = args.GetString(ARG_TOKENIZER_URL) ?? string.Empty;
                request = (CCTokenizerRequest)args.GetSerializable(ARG_REQUEST);
                supportedCardTypes = 
                    args.GetStringArrayList(ARG_SUPPORTED_CARD_TYPES) as List<string> 
                    ?? new List<string>();
                config = (CreditcardTokenizerConfig)args.GetSerializable(ARG_CONFIG);
            }
            else
            {
                throw new ArgumentException("Fragment arguments cannot be null");
            }

            SetupWebView();
        }

        private void SetupWebView()
        {
            var s = webView.Settings;
            s.JavaScriptEnabled = true;
            s.DomStorageEnabled = true;
            webView.SetWebChromeClient(new WebChromeClient());
            webView.SetWebViewClient(new InternalWebViewClient(this));
            s.MixedContentMode = MixedContentHandling.AlwaysAllow;
            webView.AddJavascriptInterface(new WebAppInterface(this), "AndroidInterface");

            webView.LoadUrl(tokenizerUrl);
        }

        private class InternalWebViewClient : WebViewClient
        {
            private readonly CreditcardTokenizerFragment parent;
            public InternalWebViewClient(CreditcardTokenizerFragment p) { parent = p; }

            public override void OnPageFinished(AWebView view, string url)
            {
                base.OnPageFinished(view, url);
                parent.MakeScriptToLoadPayoneHostedScript();
            }
        }

        private class WebAppInterface : Java.Lang.Object
        {
            readonly CreditcardTokenizerFragment parent;

            public WebAppInterface(CreditcardTokenizerFragment frag)
            {
                parent = frag;
            }

            [JavascriptInterface]
            [Export("onScriptLoaded")]
            public void OnScriptLoaded()
            {
                parent.handler.Post(parent.MakeScriptToPopulateHTML);
            }

            [JavascriptInterface]
            [Export("onScriptError")]
            public void OnScriptError()
            {
                parent.handler.Post(() =>
                    parent.config.CreditCardCheckCallback(
                        Result<CCTokenizerResponse>.Failure(CCTokenizerError.LoadingScriptFailed)));
            }

            [JavascriptInterface]
            [Export("onPayCallback")]
            public void OnPayCallback(string response)
            {
                parent.handler.Post(() =>
                {
                    try
                    {
                        var resp = JsonSerializer.Deserialize<CCTokenizerResponse>(response);
                        parent.config.CreditCardCheckCallback(
                            Result<CCTokenizerResponse>.Success(resp));
                    }
                    catch (Exception)
                    {
                        parent.config.CreditCardCheckCallback(
                            Result<CCTokenizerResponse>.Failure(CCTokenizerError.InvalidResponse));
                    }
                });
            }
        }

        private void MakeScriptToLoadPayoneHostedScript()
        {
            const string script = @"
(function() {
    if (!document.getElementById('payone-hosted-script')) {
        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = 'https://secure.prelive.pay1-test.de/client-api/js/v1/payone_hosted_min.js';
        script.id = 'payone-hosted-script';
        script.onload = function() { window.AndroidInterface.onScriptLoaded(); };
        script.onerror = function() { window.AndroidInterface.onScriptError(); };
        document.head.appendChild(script);
    }
})();";
            webView.EvaluateJavascript(script, new MakeScriptToLoadPayoneHostedScriptValueCallback());
        }

        private void MakeScriptToPopulateHTML()
        {
            var gsonList = JsonSerializer.Serialize(supportedCardTypes);
            var cfg = new
            {
                fields = new
                {
                    cardpan = config.CardPan,
                    cardcvc2 = config.CardCvc2,
                    cardexpiremonth = config.CardExpireMonth,
                    cardexpireyear = config.CardExpireYear
                },
                defaultStyle = JsonSerializer.Serialize(config.DefaultStyles),
                autoCardtypeDetection = new
                {
                    supportedCardtypes = supportedCardTypes,
                    callback = "function(detectedCardtype){ /* styling omitted for brevity */ }"
                },
                language = (config.Language == PayoneLanguage.English
                    ? "Payone.ClientApi.Language.en"
                    : "Payone.ClientApi.Language.de"),
                error = config.Error
            };
            // Because we need inline JS objects, assemble by hand:
            string defaultStylesJs = GenerateDefaultStyleKeyValuePairs();
            var script = $@"
var supportedCardtypes = {gsonList};
var config = {{
    fields: {{
        cardpan: {JsonSerializer.Serialize(config.CardPan)},
        cardcvc2: {JsonSerializer.Serialize(config.CardCvc2)},
        cardexpiremonth: {JsonSerializer.Serialize(config.CardExpireMonth)},
        cardexpireyear: {JsonSerializer.Serialize(config.CardExpireYear)}
    }},
    defaultStyle: {{
        {defaultStylesJs}
    }},
    autoCardtypeDetection: {{
        supportedCardtypes: supportedCardtypes,
        callback: function(detectedCardtype) {{
            if (window.AndroidInterface && typeof window.AndroidInterface.onPayCallback === 'function') {{
                /* styling logic omitted */
            }}
        }}
    }},
    language: ""{(config.Language == PayoneLanguage.English
                    ? "Payone.ClientApi.Language.en"
                    : "Payone.ClientApi.Language.de")}"",
    error: ""{config.Error}""
}};
var request = {{
    request: 'creditcardcheck',
    responsetype: 'JSON',
    mode: '{request.Environment.CcTokenizerIdentifier}',
    mid: '{request.Mid}',
    aid: '{request.Aid}',
    portalid: '{request.PortalId}',
    encoding: 'UTF-8',
    storecarddata: 'yes',
    hash: '{request.GeneratedHash}'
}};
var iframes = new window.Payone.ClientApi.HostedIFrames(config, request);
window.payoneIFrames = iframes;
function payCallback(response) {{
    document.getElementById('jsonResponsePre').textContent = JSON.stringify(response, null, 2);
    if (window.AndroidInterface && 
        typeof window.AndroidInterface.onPayCallback === 'function') {{
        window.AndroidInterface.onPayCallback(JSON.stringify(response));
    }}
}};
document.getElementById('{config.SubmitButtonId}').onclick = function() {{
    if (window.payoneIFrames) {{
        window.payoneIFrames.creditCardCheck('payCallback');
    }} else {{
        alert('payoneIFrames not initialized');
    }}
}};";

            webView.EvaluateJavascript(script, new MakeScriptToPopulateHTMLValueCallback());
        }

        private string GenerateDefaultStyleKeyValuePairs()
        {
            var entries = new List<string>();
            foreach (var kv in config.DefaultStyles)
            {
                entries.Add($"\"{kv.Key}\": \"{kv.Value}\"");
            }
            return string.Join(",\n", entries);
        }

        private const string ARG_TOKENIZER_URL = "tokenizerUrl";
        private const string ARG_REQUEST = "request";
        private const string ARG_SUPPORTED_CARD_TYPES = "supportedCardTypes";
        private const string ARG_CONFIG = "config";

        public static CreditcardTokenizerFragment NewInstance(
            string tokenizerUrl,
            CCTokenizerRequest request,
            List<string> supportedCardTypes,
            CreditcardTokenizerConfig config)
        {
            var frag = new CreditcardTokenizerFragment();
            var b = new Bundle();
            b.PutString(ARG_TOKENIZER_URL, tokenizerUrl);
            b.PutSerializable(ARG_REQUEST, request);
            b.PutStringArrayList(ARG_SUPPORTED_CARD_TYPES, supportedCardTypes);
            b.PutSerializable(ARG_CONFIG, config);
            frag.Arguments = b;
            return frag;
        }

        class MakeScriptToLoadPayoneHostedScriptValueCallback : Java.Lang.Object, IValueCallback
        {
            public void OnReceiveValue(Java.Lang.Object? value)
            {
                Log.Debug("CCTokenizer", "LoadPayoneHostedScript executed: " + value);
            }
        }
        
        class MakeScriptToPopulateHTMLValueCallback : Java.Lang.Object, IValueCallback
        {
            public void OnReceiveValue(Java.Lang.Object? value)
            {
                Log.Debug("CCTokenizer", "PopulateHTML executed: " + value);
            }
        }
    }
}
