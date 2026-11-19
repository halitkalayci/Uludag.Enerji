namespace ECommerce.Application.Features.Auth.Mappers
{
    // Auth feature'ının UserOperationClaim mapping'leri burada yazılır.
    // Command/Query handler'ları mapping yapmaz; bu metotları çağırır.
    public static class UserOperationClaimMapper
    {
        public static Domain.Entities.UserOperationClaim MapToUserOperationClaim(Guid userId, Guid operationClaimId)
        {
            return new Domain.Entities.UserOperationClaim
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OperationClaimId = operationClaimId
            };
        }
    }
}
