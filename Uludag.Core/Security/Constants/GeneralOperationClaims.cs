namespace Uludag.Core.Security.Constants
{
    // Belirli bir entity'ye bağlı olmayan, sistem genelindeki operation claim'ler.
    // "Admin" claim'ine sahip kullanıcı, AuthorizationBehavior'da her ISecuredRequest'i geçer.
    public static class GeneralOperationClaims
    {
        public const string Admin = "Admin";
    }
}
