using System.Text.Json.Serialization;

namespace ECommerce.Application.Features.Auth.Commands.Login
{
    // Login'den sonra istemcinin atması gereken adım.
    // JSON'a sayı (0 / 1) değil, ad olarak yazılır: "SetupRequired" / "CodeRequired".
    [JsonConverter(typeof(JsonStringEnumConverter<MultiFactorStatus>))]
    public enum MultiFactorStatus
    {
        // MFA kurulmamış -> JWT verilmez; kullanıcı önce authenticator uygulamasını kurmalı.
        SetupRequired,

        // MFA kurulu -> JWT için authenticator kodu gerekli.
        CodeRequired
    }
}
