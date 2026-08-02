using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Avaliação inicial de risco realizada pelo atendente/agente antes da vistoria de campo.
    /// Etapa 2 do fluxo. Cria este registro → status da ocorrência avança para EM_AVALIACAO.
    /// </summary>
    [Table("avaliacoes_risco")]
    public class AvaliacaoRisco
    {
        public int Id { get; set; }

        // FK (dependente da ocorrência — 1:1)
        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

        // ── Classificação inicial ────────────────────────────────────────────────
        /// <summary>
        /// Tipificações identificadas na triagem — multi-seleção (text[]).
        /// Uma mesma ocorrência costuma acumular mais de uma (ex.: trincas +
        /// infiltração). Texto, e não enum, para aceitar as opções personalizadas
        /// do catálogo, como já ocorre em <see cref="Vistoria.TipificacaoOcorrencia"/>.
        /// </summary>
        public List<string> TipificacaoInicial { get; set; } = new();

        public GrauRisco GrauRiscoInicial { get; set; }

        // ── Triagem operacional ──────────────────────────────────────────────────
        /// <summary>Usuário que realizou a abertura/triagem da vistoria.</summary>
        public int? AbertaPorUsuarioId { get; set; }
        public Usuario? AbertaPorUsuario { get; set; }

        /// <summary>Texto de requisição de documentos ao setor competente.</summary>
        public string? RequisicaoSetorDocumento { get; set; }

        /// <summary>Indica se a ocorrência foi classificada como emergência.</summary>
        public bool Emergencia { get; set; } = false;

        // ── Auditoria ────────────────────────────────────────────────────────────
        public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }
}
