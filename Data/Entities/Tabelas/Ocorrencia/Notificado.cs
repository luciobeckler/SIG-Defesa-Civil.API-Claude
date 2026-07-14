using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Pessoa que recebeu o relatório da ocorrência — propriedade da ocorrência,
    /// não uma etapa do fluxo; pode ser registrada a qualquer momento.
    /// Relacionamento 1:N com Ocorrencia — cada notificado é uma linha separada,
    /// evitando o anti-pattern de colunas NOTIFICADO_1 / NOTIFICADO_2 da planilha.
    /// </summary>
    [Table("notificados")]
    public class Notificado
    {
        public int Id { get; set; }

        // FK (muitos notificados por ocorrência — 1:N)
        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

        // ── Dados do notificado ──────────────────────────────────────────────────
        public string Nome { get; set; } = null!;

        /// <summary>RG ou CPF do notificado (campo unificado conforme planilha).</summary>
        public string? RgCpf { get; set; }

        public DateOnly DataNotificacao { get; set; }

        /// <summary>
        /// Como o notificado recebeu o relatório. PRESENCIAL exige a coleta da
        /// assinatura (arquivo assinatura_notificado_{Id}.png na pasta Assinaturas).
        /// </summary>
        public FormaRecebimentoRelatorio FormaRecebimento { get; set; } = FormaRecebimentoRelatorio.EMAIL;

        // ── Auditoria ────────────────────────────────────────────────────────────
        public int RegistradoPorId { get; set; }
        public Usuario RegistradoPor { get; set; } = null!;
        public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
    }
}
