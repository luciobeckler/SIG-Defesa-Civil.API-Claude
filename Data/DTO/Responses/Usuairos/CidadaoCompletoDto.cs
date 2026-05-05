namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Usuairos
{
    /// <summary>
    /// Dados completos do cidadão (sem mascaramento)
    /// </summary>
    /// <summary>Dados completos do solicitante — revelados somente via endpoint LGPD.</summary>
    public class CidadaoCompletoDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string? Rg { get; set; }
        public string? OrgaoEmissor { get; set; }
        public string? Telefone { get; set; }
        public string? Celular { get; set; }
        public string Email { get; set; } = string.Empty;
    }

}
