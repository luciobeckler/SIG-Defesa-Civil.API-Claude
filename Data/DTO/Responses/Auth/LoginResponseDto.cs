namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public UsuarioResponseDto Usuario { get; set; } = null!;
    }
}
