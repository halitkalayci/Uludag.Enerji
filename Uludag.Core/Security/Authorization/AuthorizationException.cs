namespace Uludag.Core.Security.Authorization
{
    // Kullanıcı giriş yapmış (token geçerli) ama bu işlem için gereken claim'e sahip değil.
    // AuthorizationBehavior fırlatır; WebAPI'de 403'e çevrilir.
    // (401 = kim olduğunu bilmiyorum, 403 = kim olduğunu biliyorum ama izin yok.)
    public class AuthorizationException : Exception
    {
        public AuthorizationException(string message) : base(message)
        {
        }
    }
}
