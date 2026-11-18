namespace ECommerce.Application.Exceptions
{
    // "X var olmalı" kuralının ihlali. WebAPI'deki middleware'de HTTP 404'e çevrilir.
    // Handler'lar artık "bulunamadı" demek için false / null dönmez; bu istisna fırlatılır.
    public class NotFoundException : BusinessException
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
