using ECommerce.Application.Features.Product.Commands.Create;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        //                                     REFERANS UNUTMA!
        public async Task<IActionResult> Create([FromBody] CreateProductCommand createProductCommand, CancellationToken cancellationToken)
        {
            // veritabanı bağlantısı -> YANLIŞ
            // iş kodu, validation -> YANLIŞ

            // Sorumluluk -> Kullanıcı ile http üzerinden haberleş -> isteği doğru
            // handler'a ilet.

            // Yanlış bağlantı. -> Handlerı newleme, burdaki iletişimi Mediator'un (ortak iletişim ağı)
            // yapıyor olması.
            // Controller handler'ı tanımaz; isteği Mediator'a verir, Mediator doğru handler'ı bulur.
            await mediator.Send(createProductCommand, cancellationToken);
            return Ok();
        }
    }
}
