namespace Uludag.Core.Security.MultiFactor
{
    // Şifresi doğrulanmış ama MFA adımını henüz tamamlamamış kullanıcının kısa ömürlü oturumu.
    // JWT değildir: hiçbir isteğe yetki vermez, yalnızca MFA kurulum / doğrulama isteklerinde
    // "bu kişi az önce şifresini doğru girdi" kanıtı olarak kullanılır.
    // Token opak (rastgele) bir metindir; karşılığı cache'te (Redis / in-memory) durur, tek kullanımlıktır.
    public class MultiFactorSession
    {
        public string Token { get; set; } = null!;
        public Guid UserId { get; set; }
        public DateTime Expiration { get; set; } // UTC
    }
}
