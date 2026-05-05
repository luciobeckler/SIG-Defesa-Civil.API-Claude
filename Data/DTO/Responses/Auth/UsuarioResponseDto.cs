namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Auth
{
    using SIG_Defesa_Civil.API.Enums;

    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string Email { get; set; } = null!;
        public TipoUsuario TipoUsuario { get; set; }
        public string? Matricula { get; set; }
        public bool Ativo { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
