using ECommerce.Application.Features.Auth.Commands.AssignOperationClaim;
using ECommerce.Application.Features.Auth.Commands.Login;
using ECommerce.Application.Features.Auth.Commands.Register;
using ECommerce.Application.Features.Auth.Commands.SetupOtpAuthenticator;
using ECommerce.Application.Features.Auth.Commands.VerifyOtpAuthenticator;
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
            // Başarılı -> 200 + MFA token'ı ve sıradaki adım (JWT DEĞİL):
            //   SetupRequired -> MFA kurulmamış (ilk giriş): mfa/setup, sonra mfa/verify.
            //   CodeRequired  -> MFA kurulu: mfa/verify.
            // E-posta yok veya şifre yanlış -> 400 (BusinessExceptionMiddleware).
            LoginResponse loginResponse = await mediator.Send(loginCommand, cancellationToken);
            return Ok(loginResponse);
        }

        [HttpPost("mfa/setup")]
        public async Task<IActionResult> SetupOtpAuthenticator([FromBody] SetupOtpAuthenticatorCommand setupOtpAuthenticatorCommand, CancellationToken cancellationToken)
        {
            // Başarılı -> 200 + gizli anahtar ve QR adresi (otpauth://). Kullanıcı bunu authenticator uygulamasına okutur.
            // MFA token'ı geçersiz / MFA zaten aktif -> 400, kullanıcı yok -> 404.
            SetupOtpAuthenticatorResponse setupOtpAuthenticatorResponse = await mediator.Send(setupOtpAuthenticatorCommand, cancellationToken);
            return Ok(setupOtpAuthenticatorResponse);
        }

        [HttpPost("mfa/verify")]
        public async Task<IActionResult> VerifyOtpAuthenticator([FromBody] VerifyOtpAuthenticatorCommand verifyOtpAuthenticatorCommand, CancellationToken cancellationToken)
        {
            // Başarılı -> 200 + JWT (ilk kurulumda MFA da aktif olur).
            // MFA token'ı geçersiz / kurulum yok / kod yanlış / deneme hakkı bitti -> 400.
            VerifyOtpAuthenticatorResponse verifyOtpAuthenticatorResponse = await mediator.Send(verifyOtpAuthenticatorCommand, cancellationToken);
            return Ok(verifyOtpAuthenticatorResponse);
        }

        [HttpPost("assign-operation-claim")]
        public async Task<IActionResult> AssignOperationClaim([FromBody] AssignOperationClaimCommand assignOperationClaimCommand, CancellationToken cancellationToken)
        {
            // Yalnızca global Admin -> diğerleri 403 (AuthorizationBehavior).
            // Kullanıcı / claim yok -> 404, claim zaten atanmış -> 400 (BusinessExceptionMiddleware).
            await mediator.Send(assignOperationClaimCommand, cancellationToken);
            return NoContent();
        }
    }
}
