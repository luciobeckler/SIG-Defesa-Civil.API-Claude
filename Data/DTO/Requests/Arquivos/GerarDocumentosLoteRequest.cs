using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos
{
    /// <summary>
    /// Request para geração de documentos em lote
    /// </summary>
    public class GerarDocumentosLoteRequest
    {
        /// <summary>
        /// Lista de IDs das ocorrências para gerar documentos
        /// </summary>
        public List<int> OcorrenciaIds { get; set; } = new();

        /// <summary>
        /// Nome do template a ser utilizado (ex: "RelatorioVistoria", "FichaVistoria")
        /// </summary>
        public TipoArquivo TipoArquivo { get; set; }

        /// <summary>
        /// ID do usuário que está solicitando a geração
        /// </summary>
        public int UsuarioId { get; set; }
    }
}
