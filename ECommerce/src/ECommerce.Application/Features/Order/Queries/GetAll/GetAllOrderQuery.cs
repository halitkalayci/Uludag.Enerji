using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Order.Queries.GetAll
{
    // Gövdesi boş sorgu: giriş yapmış kullanıcının KENDİ siparişlerini ister.
    // Kullanıcı gövdeden değil, isteğin Bearer token'ının oturumundan alınır (başkasının siparişleri görülemez).
    // Rol (ISecuredRequest) istenmez; IAllowAnonymousRequest de DEĞİL -> AuthenticationBehavior geçerli bir JWT ister.
    // Sonuçlar:
    //   başarılı -> 200 (siparişi yoksa boş liste)
    //   token yok / geçersiz / oturumu kapanmış -> 401
    public class GetAllOrderQuery : IRequest<List<OrderResponse>>
    {
        public class GetAllOrderQueryHandler : IRequestHandler<GetAllOrderQuery, List<OrderResponse>>
        {
            // Dependency Injection
            private readonly IOrderRepository orderRepository;
            private readonly AuthBusinessRules authBusinessRules;
            private readonly ITokenSessionAccessor tokenSessionAccessor; // "kim giriş yapmış" -> kimin siparişleri.
            public GetAllOrderQueryHandler(
                IOrderRepository orderRepository,
                AuthBusinessRules authBusinessRules,
                ITokenSessionAccessor tokenSessionAccessor)
            {
                this.orderRepository = orderRepository;
                this.authBusinessRules = authBusinessRules;
                this.tokenSessionAccessor = tokenSessionAccessor;
            }

            public async Task<List<OrderResponse>> Handle(GetAllOrderQuery request, CancellationToken cancellationToken)
            {
                // İsteği yapan kullanıcının oturumu (Redis).
                TokenSession? tokenSession = await tokenSessionAccessor.GetCurrentTokenSessionAsync();

                // İş kuralları
                authBusinessRules.TokenSessionShouldExist(tokenSession);

                List<Domain.Entities.Order> orders = await orderRepository.GetAllByUserIdAsync(tokenSession.UserId, cancellationToken);

                // Mapping
                return OrderMapper.MapOrdersToResponses(orders);
            }
        }
    }
}
