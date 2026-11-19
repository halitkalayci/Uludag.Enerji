using ECommerce.Application.Features.Auth.Mappers;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.Constants;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Auth.Commands.AssignOperationClaim
{
    // Bir kullanıcıya rol (operation claim) atar. Ör: UserId = ..., OperationClaimName = "Product.Read".
    // Yalnızca global "Admin" claim'ine sahip kullanıcı çalıştırabilir.
    // Sonuçlar: başarılı -> 204, kullanıcı / claim yok -> 404, claim zaten atanmış -> 400,
    //           giriş yok -> 401, Admin değil -> 403.
    // Yeni rol anında geçerlidir: atamadan sonra kullanıcının Redis'teki yetki kaydı güncellenir,
    // açık olan bütün token'ları yeniden login gerekmeden bir sonraki istekte yeni rolle çalışır.
    public class AssignOperationClaimCommand : IRequest, ISecuredRequest
    {
        // Yetki: yalnızca global Admin. AuthorizationBehavior handler'dan ÖNCE çalışır;
        // Admin olmayan biri kullanıcının / claim'in var olup olmadığını da öğrenemez (her durumda 403).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.
        string[] ISecuredRequest.Roles => [GeneralOperationClaims.Admin];

        public Guid UserId { get; set; }
        public string OperationClaimName { get; set; } = null!;

        public class AssignOperationClaimCommandHandler : IRequestHandler<AssignOperationClaimCommand>
        {
            // Dependency Injection
            private readonly AuthBusinessRules authBusinessRules;
            private readonly IOperationClaimRepository operationClaimRepository;
            private readonly IUserOperationClaimRepository userOperationClaimRepository;
            private readonly ITokenHelper tokenHelper; // Uludag.Core -> oturumların nerede tutulduğunu (Redis) handler bilmez.
            public AssignOperationClaimCommandHandler(
                AuthBusinessRules authBusinessRules,
                IOperationClaimRepository operationClaimRepository,
                IUserOperationClaimRepository userOperationClaimRepository,
                ITokenHelper tokenHelper)
            {
                this.authBusinessRules = authBusinessRules;
                this.operationClaimRepository = operationClaimRepository;
                this.userOperationClaimRepository = userOperationClaimRepository;
                this.tokenHelper = tokenHelper;
            }

            public async Task Handle(AssignOperationClaimCommand request, CancellationToken cancellationToken)
            {
                // İş kuralları
                await authBusinessRules.UserShouldExist(request.UserId, cancellationToken);

                Domain.Entities.OperationClaim? operationClaim = await operationClaimRepository.GetByNameAsync(request.OperationClaimName, cancellationToken);
                authBusinessRules.OperationClaimShouldExist(operationClaim);

                await authBusinessRules.UserCannotHaveOperationClaim(request.UserId, operationClaim.Id, cancellationToken);

                // Mapper
                Domain.Entities.UserOperationClaim userOperationClaim = UserOperationClaimMapper.MapToUserOperationClaim(request.UserId, operationClaim.Id);

                // Veritabanı
                await userOperationClaimRepository.AddAsync(userOperationClaim, cancellationToken);

                // Aktif oturumlar -> DB'deki güncel yetki listesinin tamamı Redis'e yazılır (anında geçerli).
                // Liste DB'den okunur, cache'teki eski listeye ekleme yapılmaz: kaynak her zaman veritabanıdır.
                List<string> operationClaims = await userOperationClaimRepository.GetOperationClaimNamesByUserIdAsync(request.UserId, cancellationToken);
                await tokenHelper.RefreshOperationClaimsAsync(request.UserId, operationClaims);
            }
        }
    }
}
