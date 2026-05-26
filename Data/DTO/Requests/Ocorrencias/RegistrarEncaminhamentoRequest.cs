using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Encaminhamentos formais e entrega do relatório — Etapa 6.
    /// Ao registrar, o status avança para ENCERRADA.
    /// </summary>
    public class RegistrarEncaminhamentoRequest
    {
        /// <summary>Órgãos/ações para onde a ocorrência foi encaminhada — multi-select.</summary>
        public List<Encaminhamento> Encaminhamentos { get; set; } = new();

        /// <summary>
        /// Retorno dos encaminhamentos. NÃO é coletado no registro inicial da Etapa 6.
        /// Preenchido em endpoint separado, quando o encaminhamento é dado como concluído.
        /// </summary>
        public string? RetornoEncaminhamentos { get; set; }

        /// <summary>
        /// ID do arquivo de relatório previamente enviado (TipoArquivo = RELATORIO_FINAL).
        /// </summary>
        public int? RelatorioVistoriaId { get; set; }

        [Required] public CanalEntregaRelatorio EntregaRelatorio { get; set; }
    }
}
