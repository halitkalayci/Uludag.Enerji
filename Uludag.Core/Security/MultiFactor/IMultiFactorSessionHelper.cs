namespace Uludag.Core.Security.MultiFactor
{
    // Login'in iki adımı arasındaki köprü:
    //   1) Şifre doğru -> CreateSessionAsync -> istemciye MFA token'ı (JWT değil).
    //   2) İstemci bu token + authenticator kodu ile gelir -> GetSessionAsync -> kod doğruysa JWT, RemoveSessionAsync.
    // Ayrıca kod denemelerini kullanıcı başına sınırlar (kaba kuvvet koruması).
    public interface IMultiFactorSessionHelper
    {
        // Yeni, tahmin edilemez bir token üretir ve cache'e SessionExpiration süresiyle yazar.
        Task<MultiFactorSession> CreateSessionAsync(Guid userId);

        // Token'ın oturumu. Token yanlış / süresi dolmuş / kullanılmışsa null.
        Task<MultiFactorSession?> GetSessionAsync(string token);

        // Oturumu siler -> token tekrar kullanılamaz (başarılı doğrulamadan sonra çağrılır).
        Task RemoveSessionAsync(string token);

        // Bir doğrulama denemesi hakkı kullanır. Pencere içindeki hak bittiyse false.
        // Kod kontrolünden ÖNCE çağrılır: sayaç atomik arttığı için aynı anda gönderilen istekler de hakkı aşamaz.
        Task<bool> TryConsumeVerificationAttemptAsync(Guid userId);

        // Başarılı doğrulamadan sonra sayaç sıfırlanır.
        Task ResetVerificationAttemptsAsync(Guid userId);
    }
}
