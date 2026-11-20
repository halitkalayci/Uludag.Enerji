using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Services.PaymentService
{
    // Ödeme sağlayıcısının Application'daki sözleşmesi (spec 0008).
    // Application yalnızca bu arayüzü bilir; gerçek (ya da fake) sağlayıcı ECommerce.Infrastructure'da yaşar (F-2).
    public interface IPaymentService
    {
        // Tutarı tahsil etmeyi dener. Ret bir istisna değil, sonuçtur: kararı OrderBusinessRules verir.
        Task<PaymentResult> PayAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    }

    // Tahsil edilecek tutar ve kimin adına. Tutarı her zaman sunucu hesaplar (BR-15), istemciden alınmaz.
    public sealed record PaymentRequest(Guid UserId, Money Amount);

    // Sağlayıcının cevabı: onaylandı mı?
    public sealed record PaymentResult(bool IsApproved);
}
