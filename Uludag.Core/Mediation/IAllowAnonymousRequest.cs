namespace Uludag.Core.Mediation
{
    /// <summary>
    /// İşaret (marker) arayüzü: bu arayüzü uygulayan istekler giriş yapılmadan çalıştırılabilir.
    /// AuthenticationBehavior bu istekler için JWT doğrulaması yapmaz (ör. Login, Register).
    /// </summary>
    public interface IAllowAnonymousRequest
    {
    }
}
