using SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Usuarios;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// DTO principal para abertura de ocorrência.
    /// No controller, os dados estruturados virão como JSON e os arquivos como IFormFile separados.
    /// </summary>
    public class CriarOcorrenciaRequest
    {
        public CidadaoDto Cidadao { get; set; } = null!;
        public LocalOcorrenciaDto Local { get; set; } = null!;
        public string DescricaoProblema { get; set; } = string.Empty;

        /// <summary>
        /// Lista de arquivos que será populada manualmente no controller
        /// a partir dos IFormFile recebidos via multipart/form-data
        /// </summary>
        public List<ArquivoUploadDto> Arquivos { get; set; } = new();
    }
}
