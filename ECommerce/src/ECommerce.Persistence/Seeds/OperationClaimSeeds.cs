using ECommerce.Application.Features.Category.Constants;
using ECommerce.Application.Features.Order.Constants;
using ECommerce.Application.Features.OrderItem.Constants;
using ECommerce.Application.Features.Product.Constants;
using ECommerce.Domain.Entities;
using Uludag.Core.Security.Constants;

namespace ECommerce.Persistence.Seeds
{
    // Sistemde tanımlı tüm operation claim'ler (HasData ile migration'a yazılır).
    // Id'ler sabittir: her build'de yeniden üretilseydi EF her migration'da kayıtları silip tekrar eklemeye çalışırdı.
    // Claim adları Application'daki sabitlerden gelir; metin burada tekrar yazılmaz.
    public static class OperationClaimSeeds
    {
        public static readonly Guid AdminId = Guid.Parse("3ec75534-443a-49fd-a426-d9c1ca8e0257");

        public static readonly OperationClaim[] All =
        [
            new() { Id = AdminId, Name = GeneralOperationClaims.Admin },

            new() { Id = Guid.Parse("0f88c5f4-b720-4506-b6cc-901df4c9c148"), Name = ProductOperationClaims.Admin },
            new() { Id = Guid.Parse("22bba1ae-46ab-412c-a82a-b3dfa42252c2"), Name = ProductOperationClaims.Write },
            new() { Id = Guid.Parse("d0894728-57ef-430c-aa75-395af47a209c"), Name = ProductOperationClaims.Read },
            new() { Id = Guid.Parse("cf499e30-e50d-4750-9274-9d376187c422"), Name = ProductOperationClaims.Create },
            new() { Id = Guid.Parse("10a607ca-391b-48c0-adf9-d615d87863f4"), Name = ProductOperationClaims.Update },
            new() { Id = Guid.Parse("92c2c10a-ad94-41d9-a946-519e00abafbc"), Name = ProductOperationClaims.Delete },

            new() { Id = Guid.Parse("b3ec174f-eb56-4b27-9cc0-b3e7a0c584ac"), Name = OrderOperationClaims.Admin },
            new() { Id = Guid.Parse("b3fdabda-cb8e-4b08-8cdb-344789bbeb86"), Name = OrderOperationClaims.Write },
            new() { Id = Guid.Parse("74095b97-f2e2-4afc-b97a-eac89510afdb"), Name = OrderOperationClaims.Read },
            new() { Id = Guid.Parse("e6a68171-dd72-432a-ad1a-5cf3a40026d1"), Name = OrderOperationClaims.Create },
            new() { Id = Guid.Parse("2ac55953-92fc-4ed2-ae6e-2bb5f079fd16"), Name = OrderOperationClaims.Update },
            new() { Id = Guid.Parse("a262973a-f85d-4f28-8086-61e7824812a2"), Name = OrderOperationClaims.Delete },

            new() { Id = Guid.Parse("76115df6-7f55-42d5-8c6b-c7b7ea0d6327"), Name = OrderItemOperationClaims.Admin },
            new() { Id = Guid.Parse("24b8bac0-7fc2-4550-994c-f6e22ba7af94"), Name = OrderItemOperationClaims.Write },
            new() { Id = Guid.Parse("8d26828e-7cc9-4364-b93e-b47038774042"), Name = OrderItemOperationClaims.Read },
            new() { Id = Guid.Parse("fcc18799-a665-4f8e-adc2-4ae0f6a08155"), Name = OrderItemOperationClaims.Create },
            new() { Id = Guid.Parse("6e818194-6bc9-4c80-8c14-03219b616261"), Name = OrderItemOperationClaims.Update },
            new() { Id = Guid.Parse("21ba4447-0d0d-4242-935d-8087718d36b5"), Name = OrderItemOperationClaims.Delete },

            new() { Id = Guid.Parse("65f7e5d9-221d-416a-b447-165ee25e0174"), Name = CategoryOperationClaims.Admin },
            new() { Id = Guid.Parse("8d887ad1-4f78-45f7-af05-39528dd5f057"), Name = CategoryOperationClaims.Write },
            new() { Id = Guid.Parse("3b9a774d-3d8b-419e-adef-2a0d3909875b"), Name = CategoryOperationClaims.Read },
            new() { Id = Guid.Parse("646cc778-65fe-4f48-baf9-eafd50912044"), Name = CategoryOperationClaims.Create },
            new() { Id = Guid.Parse("a6d3bc82-3a7e-4bb1-93b8-7b6eb8254754"), Name = CategoryOperationClaims.Update },
            new() { Id = Guid.Parse("cd6c5aba-a7d4-42be-b9cb-f3f962d5d723"), Name = CategoryOperationClaims.Delete },
        ];
    }
}
