namespace ECommerce.Domain.Entities
{
    // Bir işlemi yapma yetkisi (ör. "Product.Create"). Kullanıcılarla çoka-çok ilişkilidir (UserOperationClaim).
    public class OperationClaim
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        // Navigation Properties
        public virtual ICollection<UserOperationClaim> UserOperationClaims { get; set; } = new List<UserOperationClaim>();
    }
}
