using ECommerce.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;

namespace ECommerce.WebAPI.Middlewares
{
    // Rules sınıflarının fırlattığı istisnaları HTTP cevabına (ProblemDetails) çevirir.
    // Handler'lar "bulunamadı" demek için false / null dönmez; kural ihlali istisna olarak buraya kadar çıkar.
    //   NotFoundException -> 404
    //   BusinessException -> 400
    // Diğer (beklenmeyen) istisnalara dokunulmaz -> 500.
    public class BusinessExceptionMiddleware
    {
        private readonly RequestDelegate next;
        public BusinessExceptionMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            // Sıra önemli: NotFoundException, BusinessException'dan türer; önce o yakalanmalı.
            catch (NotFoundException exception)
            {
                await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", exception.Message);
            }
            catch (BusinessException exception)
            {
                await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Business Rule Violation", exception.Message);
            }
        }

        private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
        {
            ProblemDetails problem = new()
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}
