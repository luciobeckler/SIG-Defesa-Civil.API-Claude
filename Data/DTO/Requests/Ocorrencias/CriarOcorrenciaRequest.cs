using SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Usuarios;
using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// DTO principal para abertura de ocorrência — Etapa 1.
    /// Dados estruturados chegam como JSON; arquivos como IFormFile via multipart/form-data.
    /// </summary>
    public class CriarOcorrenciaRequest
    {
        [Required] public CidadaoDto Cidadao { get; set; } = null!;
        [Required] public LocalOcorrenciaDto Local { get; set; } = null!;

        [Required][MinLength(10, ErrorMessage = "Descrição deve ter no mínimo 10 caracteres.")]
        public string DescricaoProblema { get; set; } = string.Empty;

        /// <summary>
        /// Populado pelo controller a partir dos IFormFile recebidos.
        /// Deve conter pelo menos o comprovante de residência.
        /// </summary>
        public List<ArquivoUploadDto> Arquivos { get; set; } = new();
    }
}
