using ECommerce.Application.Features.Product.Commands.Create;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        [HttpPost]
        //                                     REFERANS UNUTMA!
        public IActionResult Create([FromBody] CreateProductCommand createProductCommand)
        {
            // veritabanı bağlantısı -> YANLIŞ
            // iş kodu, validation -> YANLIŞ

            // Sorumluluk -> Kullanıcı ile http üzerinden haberleş -> isteği doğru
            // handler'a ilet.

            // Yanlış bağlantı. -> Handlerı newleme, burdaki iletişimi Mediator'un (ortak iletişim ağı)
            // yapıyor olması.
            CreateProductCommand.CreateProductCommandHandler handler = new();
            handler.Handle(createProductCommand);
            return Ok();
        }
    }
}
