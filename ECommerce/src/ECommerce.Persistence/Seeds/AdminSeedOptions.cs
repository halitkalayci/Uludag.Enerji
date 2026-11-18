namespace ECommerce.Persistence.Seeds
{
    // appsettings'teki "AdminSeed" bölümü. Şifre repoya yazılmaz:
    // Development'ta appsettings.Development.json, diğer ortamlarda AdminSeed__Email / AdminSeed__Password ortam değişkenleri.
    // Bölüm yoksa / boşsa seeder hiçbir şey yapmaz.
    public class AdminSeedOptions
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
