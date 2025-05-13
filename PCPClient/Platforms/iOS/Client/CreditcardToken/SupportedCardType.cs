using ObjCRuntime;

namespace Com.Payone.PcpClientSdk.iOS;

[Native]
public enum SupportedCardType : long
{
    Visa,
    MasterCard,
    AmericanExpress,
    DinersClub,
    Jcb,
    MaestroInternational,
    ChinaUnionPay,
    Uatp,
    Girocard
}

public static class SupportedCardTypeExtensions
{
    public static string Identifier(this SupportedCardType c) => c switch
    {
        SupportedCardType.Visa                   => "V",
        SupportedCardType.MasterCard             => "M",
        SupportedCardType.AmericanExpress        => "A",
        SupportedCardType.DinersClub             => "D",
        SupportedCardType.Jcb                    => "J",
        SupportedCardType.MaestroInternational  => "O",
        SupportedCardType.ChinaUnionPay          => "P",
        SupportedCardType.Uatp                   => "U",
        SupportedCardType.Girocard               => "G",
        _                                         => ""
    };
}