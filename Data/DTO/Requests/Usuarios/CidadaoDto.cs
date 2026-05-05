using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Usuarios
{
    /// <summary>DTO de identificação do solicitante — Etapa 1.</summary>
    public class CidadaoDto
    {
        [Required] public string Nome { get; set; } = string.Empty;
        [Required] public string Cpf { get; set; } = string.Empty;

        public string? Rg { get; set; }
        /// <summary>Órgão emissor do RG (ex: SSP/MG).</summary>
        public string? OrgaoEmissor { get; set; }

        public string? Telefone { get; set; }
        public string? Celular { get; set; }

        [Required] public string Email { get; set; } = string.Empty;
    }
}
