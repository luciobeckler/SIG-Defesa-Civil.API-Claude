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

        /// <summary>
        /// Opcional — o chamado pode ser registrado na hora e detalhado depois.
        /// </summary>
        /// <remarks>
        /// ⚠️ Este DTO chega como JSON dentro do campo <c>Dados</c> do multipart e é
        /// desserializado <b>à mão</b> no controller — o ModelState não roda sobre ele,
        /// então os DataAnnotations daqui <b>não são validados</b>. Valem só para o
        /// Swagger; quem reprova de fato é <c>OcorrenciaController.ValidarRequest</c>.
        /// O mínimo de 10 caracteres é cobrado pelo formulário, e pela API apenas na
        /// edição (<c>AtualizarOcorrenciaRequest</c>, essa sim vinda de <c>[FromBody]</c>).
        /// </remarks>
        [MinLength(10, ErrorMessage = "Descrição deve ter no mínimo 10 caracteres.")]
        public string? DescricaoProblema { get; set; }

        /// <summary>
        /// Populado pelo controller a partir dos IFormFile recebidos.
        /// Deve conter pelo menos o comprovante de residência.
        /// </summary>
        public List<ArquivoUploadDto> Arquivos { get; set; } = new();
    }
}
