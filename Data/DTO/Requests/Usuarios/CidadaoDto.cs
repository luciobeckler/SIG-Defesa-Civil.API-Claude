using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Usuarios
{
    /// <summary>DTO de identificação do solicitante — Etapa 1.</summary>
    public class CidadaoDto
    {
        [Required] public string Nome { get; set; } = string.Empty;

        /// <summary>Opcional — o solicitante nem sempre tem o documento em mãos.</summary>
        public string? Cpf { get; set; }

        public string? Rg { get; set; }
        /// <summary>Órgão emissor do RG (ex: SSP/MG).</summary>
        public string? OrgaoEmissor { get; set; }

        public string? Telefone { get; set; }
        public string? Celular { get; set; }

        /// <summary>Opcional — boa parte dos solicitantes só deixa telefone.</summary>
        public string? Email { get; set; }
    }
}
