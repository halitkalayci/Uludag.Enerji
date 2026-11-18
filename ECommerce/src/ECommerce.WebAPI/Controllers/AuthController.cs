using ECommerce.Application.Features.Auth.Commands.Login;
using ECommerce.Application.Features.Auth.Commands.Register;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IMediator mediator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand registerCommand, CancellationToken cancellationToken)
        {
            // E-posta zaten kayıtlıysa kural ihlali -> 400 (BusinessExceptionMiddleware).
            await mediator.Send(registerCommand, cancellationToken);
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand loginCommand, CancellationToken cancellationToken)
        {
            // Başarılı -> 200 + JWT. E-posta yok veya şifre yanlış -> 400 (BusinessExceptionMiddleware).
            LoginResponse loginResponse = await mediator.Send(loginCommand, cancellationToken);
            return Ok(loginResponse);
        }
    }
}
