namespace ECommerce.Infrastructure.Payment
{
    // Fake ödeme sağlayıcısının ayarları — appsettings'teki "FakePayment" bölümü (spec 0008, K-5).
    public class FakePaymentOptions
    {
        public const string SectionName = "FakePayment";

        // Bir ödemenin reddedilme olasılığı, 0..1. 0 = her zaman onay, 1 = her zaman ret.
        // Testler ve demolar sonucu bu değerle sabitler. Olasılıktır, para değildir (Money kuralı dışında).
        public double DeclineRate { get; set; } = 0.25;
    }
}
