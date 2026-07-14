using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Encaminhamentos formais e entrega do relatório — Etapa 6 e encerramento do ciclo.
    /// Cria este registro → status da ocorrência avança para ENCERRADA.
    /// </summary>
    [Table("encaminhamentos_finais")]
    public class EncaminhamentoFinal
    {
        public int Id { get; set; }

        // FK (dependente da ocorrência — 1:1)
        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

        // ── Encaminhamentos formais ──────────────────────────────────────────────
        /// <summary>
        /// Órgãos/ações para onde a ocorrência foi encaminhada — multi-select (integer[]).
        /// Compartilha o enum <see cref="Encaminhamento"/> com a Vistoria.
        /// </summary>
        public List<Encaminhamento> Encaminhamentos { get; set; } = new();

        /// <summary>
        /// Retorno/conclusão dos encaminhamentos.
        /// Preenchido em momento posterior, quando o encaminhamento é dado como concluído.
        /// </summary>
        public string? RetornoEncaminhamentos { get; set; }

        // ── Relatório de vistoria ────────────────────────────────────────────────
        /// <summary>FK para o arquivo do relatório (Arquivo.TipoArquivo = RELATORIO_FINAL).</summary>
        public int? RelatorioVistoriaId { get; set; }
        public Arquivo? RelatorioVistoria { get; set; }

        // A entrega do relatório ao solicitante é sempre por e-mail — não há canal configurável.

        // ── Auditoria ────────────────────────────────────────────────────────────
        public int RegistradoPorId { get; set; }
        public Usuario RegistradoPor { get; set; } = null!;
        public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }
}
