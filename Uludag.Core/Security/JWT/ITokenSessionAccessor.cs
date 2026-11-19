namespace Uludag.Core.Security.JWT
{
    // O anki HTTP isteğinin token oturumunu verir ("şu an kim giriş yapmış?").
    // Handler'lar Authorization başlığını, JWT'yi veya HttpContext'i bilmez; yalnızca bu arayüzü kullanır.
    public interface ITokenSessionAccessor
    {
        // İsteğin Bearer token'ı doğrulanır ve cache'teki oturumu döner.
        // Token yok / geçersiz / süresi dolmuş / oturumu silinmişse null.
        Task<TokenSession?> GetCurrentTokenSessionAsync();
    }
}
