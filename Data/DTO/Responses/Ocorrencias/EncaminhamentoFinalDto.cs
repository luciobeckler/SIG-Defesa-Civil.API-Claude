using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>Encaminhamentos formais e entrega do relatório — resposta da Etapa 6.</summary>
    public class EncaminhamentoFinalDto
    {
        public int Id { get; set; }
        public List<Encaminhamento> Encaminhamentos { get; set; } = new();

        /// <summary>
        /// Null enquanto o encaminhamento não for dado como concluído.
        /// Preenchido em endpoint separado.
        /// </summary>
        public string? RetornoEncaminhamentos { get; set; }

        /// <summary>Metadados do relatório anexado (se houver).</summary>
        public DocumentoVisualizacao? RelatorioVistoria { get; set; }

        public CanalEntregaRelatorio EntregaRelatorio { get; set; }

        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime RegistradoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }
}
