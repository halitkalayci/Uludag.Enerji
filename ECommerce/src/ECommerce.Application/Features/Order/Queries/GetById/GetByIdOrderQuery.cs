using ECommerce.Application.Exceptions;
using ECommerce.Application.Features.Order.Constants;
using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.Repositories;
using Microsoft.AspNetCore.Http;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Order.Queries.GetById
{
    // Sorgu -> geriye değer döndüren istek. Sipariş yoksa NotFoundException fırlar (null dönmez).
    public class GetByIdOrderQuery : IRequest<OrderResponse>
    {
        // Yetki: Order.Admin veya Order.Read claim'lerinden biri yeterli (global Admin her zaman geçer).
        // Açık (explicit) implementasyon: public property olmaz, JSON gövdesine / Swagger'a girmez.

        public Guid Id { get; set; }

        public class GetByIdOrderQueryHandler : IRequestHandler<GetByIdOrderQuery, OrderResponse>
        {
            // Dependency Injection
            private readonly IOrderRepository orderRepository;
            private readonly OrderBusinessRules orderBusinessRules;
            private readonly ITokenSessionAccessor tokenSessionAccessor;
            public GetByIdOrderQueryHandler(IOrderRepository orderRepository, OrderBusinessRules orderBusinessRules, ITokenSessionAccessor tokenSessionAccessor)
            {
                this.orderRepository = orderRepository;
                this.orderBusinessRules = orderBusinessRules;
                this.tokenSessionAccessor = tokenSessionAccessor;
            }

            public async Task<OrderResponse> Handle(GetByIdOrderQuery request, CancellationToken cancellationToken)
            {
                Domain.Entities.Order? order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);

                // Sorgusuz sualsiz id tutuyorsa geri dön -> Broken Access
                // O order, giriş yapmış kullanıcın mı? SORSUNU SORMAK ŞART

                TokenSession? tokenSession = await tokenSessionAccessor.GetCurrentTokenSessionAsync();
                if (order.UserId != tokenSession.UserId)
                    throw new NotFoundException("Böyle bir sipariş bulunamadı.");
                //httpContextAccessor.HttpContext.User;

                orderBusinessRules.OrderShouldExistWhenSelected(order);

                // Mapping
                return OrderMapper.MapOrderToResponse(order);
            }
        }
    }
}
