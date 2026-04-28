namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Usuarios
{
    public class CidadaoDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string? Rg { get; set; }
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
