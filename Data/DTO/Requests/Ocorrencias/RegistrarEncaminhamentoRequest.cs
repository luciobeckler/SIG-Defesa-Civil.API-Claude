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
        public string? Encaminhamentos { get; set; }
        public string? RetornoEncaminhamentos { get; set; }

        /// <summary>
        /// ID do arquivo de relatório previamente enviado (TipoArquivo = RELATORIO_FINAL).
        /// </summary>
        public int? RelatorioVistoriaId { get; set; }

        [Required] public CanalEntregaRelatorio EntregaRelatorio { get; set; }
    }
}
