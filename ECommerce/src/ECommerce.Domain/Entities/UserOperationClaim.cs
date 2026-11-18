namespace ECommerce.Domain.Entities
{
    // User <-> OperationClaim çoka-çok ilişkisinin ara tablosu.
    // Açık bir entity olarak tutulur: kullanıcıya claim atama / kaldırma bu kayıt üzerinden yapılır.
    public class UserOperationClaim
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OperationClaimId { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual OperationClaim OperationClaim { get; set; } = null!;
    }
}
