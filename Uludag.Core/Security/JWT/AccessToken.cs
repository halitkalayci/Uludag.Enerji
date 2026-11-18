namespace Uludag.Core.Security.JWT
{
    // Üretilen token ve ne zamana kadar geçerli olduğu.
    public class AccessToken
    {
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; } // UTC
    }
}
