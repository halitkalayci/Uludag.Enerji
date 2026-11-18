namespace ECommerce.Application.Exceptions
{
    // Bir iş kuralı ihlal edildiğinde Rules sınıfları bu istisnayı fırlatır.
    // Handler'lar kural sonucunu if-else ile kontrol etmez; kural ihlali istisna olarak
    // WebAPI'deki middleware'e kadar çıkar ve orada HTTP 400'e çevrilir.
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message)
        {
        }
    }
}
