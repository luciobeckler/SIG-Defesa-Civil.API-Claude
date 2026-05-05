using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>Endereço completo do imóvel afetado — Etapa 1.</summary>
    public class LocalOcorrenciaDto
    {
        [Required] public string Endereco { get; set; } = string.Empty;
        [Required] public string Bairro { get; set; } = string.Empty;
        public string? Numero { get; set; }

        [StringLength(8, MinimumLength = 8, ErrorMessage = "CEP deve ter 8 dígitos.")]
        public string? Cep { get; set; }

        public string? Complemento { get; set; }

        [Required] public string Cidade { get; set; } = string.Empty;

        [Required][StringLength(2, MinimumLength = 2, ErrorMessage = "UF deve ter 2 caracteres.")]
        public string Uf { get; set; } = string.Empty;

        /// <summary>Coordenada GPS em texto livre (ex: "-19.8822, -43.8922").</summary>
        public string? Coordenada { get; set; }
        public string? Referencia { get; set; }
        public string? NumeroIptu { get; set; }
    }
}
