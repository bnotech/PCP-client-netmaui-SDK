using System.Text.Json;
using Foundation;
using PassKit;

namespace Com.Payone.PcpClientSdk.iOS;

public class ApplePayHandler : NSObject, IPKPaymentAuthorizationControllerDelegate
{
    public PKPaymentAuthorizationController PaymentController { get; private set; }
    public Action<PKPaymentAuthorizationResult> PaymentAuthorized { get; set; }
    public Action<PKContact> SelectedShippingContact { get; set; }
    public Func<PKShippingMethod, PKPaymentRequestShippingMethodUpdate> OnShippingMethodDidChange { get; set; }
    public Func<PKPaymentMethod, PKPaymentRequestPaymentMethodUpdate> OnDidSelectPaymentMethod { get; set; }
    public Func<string, PKPaymentRequestCouponCodeUpdate> OnChangeCouponCode { get; set; }

    PKPaymentAuthorizationStatus paymentStatus = PKPaymentAuthorizationStatus.Failure;
    Action<bool> completion;
    readonly NSUrl processPaymentServerUrl;
    readonly NSUrlSession session;
    PKPaymentRequest request;

    public ApplePayHandler(NSUrl processPaymentServerUrl, NSUrlSession urlSession = null)
    {
        this.processPaymentServerUrl = processPaymentServerUrl;
        session = urlSession ?? NSUrlSession.SharedSession;
    }

    public bool SupportsApplePay()
    {
        if (request == null)
            return PKPaymentAuthorizationController.CanMakePayments;
        return PKPaymentAuthorizationController.CanMakePayments &&
               request.SupportedNetworks != null &&
               PKPaymentAuthorizationController.CanMakePaymentsUsingNetworks(
                   request.SupportedNetworks.Select(ns => ns.ToString()).ToArray(), request.MerchantCapabilities);
    }

    public void StartPayment(
        PKPaymentRequest request,
        Func<PKPaymentMethod, PKPaymentRequestPaymentMethodUpdate> onDidSelectPaymentMethod,
        Action<bool> completion
    )
    {
        PCPLogger.Info("Starts payment request.");
        this.request = request;
        this.completion = completion;
        OnDidSelectPaymentMethod = onDidSelectPaymentMethod;
        PaymentController = new PKPaymentAuthorizationController(request);
        PaymentController.Delegate = this;
        PaymentController.Present(b => { });
    }

    [Export("paymentAuthorizationControllerDidFinish:")]
    public void DidFinish(PKPaymentAuthorizationController controller)
    {
        PCPLogger.Info("Dismisses PKPaymentAuthorizationController.");
        controller.Dismiss(() => { });
        completion?.Invoke(paymentStatus == PKPaymentAuthorizationStatus.Success);
    }

    [Export("paymentAuthorizationController:didAuthorizePayment:handler:")]
    public void DidAuthorizePayment(
        PKPaymentAuthorizationController controller,
        PKPayment payment,
        Action<PKPaymentAuthorizationResult> handler
    )
    {
        var pm = payment.Token.PaymentMethod.Network ?? "";
        var txId = payment.Token.TransactionIdentifier;
        var data = new Dictionary<string, string>
        {
            ["paymentMethod"] = pm,
            ["transactionIdentifier"] = txId
        };
        // 1. Serialisiere dein Dictionary in einen JSON-String
        string json = JsonSerializer.Serialize(data);

// 2. Wandle den JSON-String in ein NSData-Objekt um (UTF-8)
        var jsonData = NSData.FromString(json, NSStringEncoding.UTF8);

// 3. Baue den Request
        var req = new NSMutableUrlRequest(processPaymentServerUrl)
        {
            HttpMethod = "POST",
            Body = jsonData
        };
// 4. Setze den Content-Type-Header
        req["Content-Type"] = "application/json";

        var task = session.CreateDataTask(req, (nsData, resp, err) =>
        {
            if (err != null)
            {
                PCPLogger.Error(err.LocalizedDescription);
                paymentStatus = PKPaymentAuthorizationStatus.Failure;
                var res = new PKPaymentAuthorizationResult(PKPaymentAuthorizationStatus.Failure, new NSError[] { err });
                handler(res);
                PaymentAuthorized?.Invoke(res);
            }
            else
            {
                var str = NSString.FromData(nsData, NSStringEncoding.UTF8);
                PCPLogger.Info("Received data:\n" + str);
                paymentStatus = PKPaymentAuthorizationStatus.Success;
                var res = new PKPaymentAuthorizationResult(PKPaymentAuthorizationStatus.Success, null);
                handler(res);
                PaymentAuthorized?.Invoke(res);
            }
        });
        task.Resume();
    }

    [Export("paymentAuthorizationController:didSelectShippingMethod:handler:")]
    public void DidSelectShippingMethod(
        PKPaymentAuthorizationController controller,
        PKShippingMethod shippingMethod,
        Action<PKPaymentRequestShippingMethodUpdate> handler
    )
    {
        if (OnShippingMethodDidChange == null)
        {
            PCPLogger.Error("No OnShippingMethodDidChange defined.");
            completion?.Invoke(false);
            return;
        }

        PCPLogger.Info("Shipping method selected.");
        handler(OnShippingMethodDidChange(shippingMethod));
    }

    [Export("paymentAuthorizationController:didSelectShippingContact:handler:")]
    public void DidSelectShippingContact(
        PKPaymentAuthorizationController controller,
        PKContact contact,
        Action<PKPaymentRequestShippingContactUpdate> handler
    )
    {
        if (request == null)
        {
            PCPLogger.Error("No request set on shipping contact change.");
            completion?.Invoke(false);
            return;
        }

        SelectedShippingContact?.Invoke(contact);
        handler(new PKPaymentRequestShippingContactUpdate(null, request.PaymentSummaryItems, request.ShippingMethods));
    }

    [Export("paymentAuthorizationController:didChangeCouponCode:handler:")]
    public void DidChangeCouponCode(
        PKPaymentAuthorizationController controller,
        string couponCode,
        Action<PKPaymentRequestCouponCodeUpdate> handler
    )
    {
        if (OnChangeCouponCode == null)
        {
            PCPLogger.Error("No OnChangeCouponCode defined.");
            completion?.Invoke(false);
            return;
        }

        handler(OnChangeCouponCode(couponCode));
    }

    [Export("paymentAuthorizationController:didSelectPaymentMethod:handler:")]
    public void DidSelectPaymentMethod(
        PKPaymentAuthorizationController controller,
        PKPaymentMethod paymentMethod,
        Action<PKPaymentRequestPaymentMethodUpdate> handler
    )
    {
        if (OnDidSelectPaymentMethod == null)
        {
            PCPLogger.Error("No OnDidSelectPaymentMethod defined.");
            completion?.Invoke(false);
            return;
        }

        handler(OnDidSelectPaymentMethod(paymentMethod));
    }
}