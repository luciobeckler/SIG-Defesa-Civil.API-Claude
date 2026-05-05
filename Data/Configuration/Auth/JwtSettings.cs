namespace SIG_Defesa_Civil.API.Data.Configuration.Auth
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpirationHours { get; set; } = 8;
    }
}
