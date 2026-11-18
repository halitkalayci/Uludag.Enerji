namespace Uludag.Core.Mediation
{
    /// <summary>
    /// Yetki gerektiren istek (Command / Query). AuthorizationBehavior bu istekler için
    /// kullanıcının token'ındaki operation claim'leri <see cref="Roles"/> ile karşılaştırır.
    /// </summary>
    /// <remarks>
    /// <see cref="Roles"/>'tan en az biri kullanıcıda varsa (birebir metin eşleşmesi) istek geçer.
    /// Global "Admin" claim'i her zaman geçer. İsteklerde açık (explicit) implementasyon önerilir:
    /// <c>string[] ISecuredRequest.Roles => [...]</c> -> public property olmaz, JSON gövdesine / Swagger'a girmez.
    /// </remarks>
    public interface ISecuredRequest
    {
        string[] Roles { get; }
    }
}
