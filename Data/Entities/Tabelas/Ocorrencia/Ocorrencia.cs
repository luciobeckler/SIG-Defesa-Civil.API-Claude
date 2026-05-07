using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Âncora do ciclo de vida. Contém apenas os dados da Etapa 1 (abertura)
    /// e o status que representa em qual etapa a ocorrência se encontra.
    /// Dados das etapas 2–6 ficam nas entidades filhas (1:1 ou 1:N).
    /// </summary>
    [Table("ocorrencias")]
    public class Ocorrencia
    {
        public int Id { get; set; }

        /// <summary>Protocolo gerado via sequence PostgreSQL. Formato: YYYY-XXXX.</summary>
        public string Protocolo { get; set; } = null!;

        // ── Etapa 1: Solicitante ─────────────────────────────────────────────────
        public int SolicitanteId { get; set; }
        public Usuario Solicitante { get; set; } = null!;

        // ── Etapa 1: Descrição do problema ───────────────────────────────────────
        public string DescricaoProblema { get; set; } = null!;

        // ── Máquina de estados ───────────────────────────────────────────────────
        public StatusOcorrencia Status { get; set; } = StatusOcorrencia.ABERTA;

        // ── Auditoria de criação ─────────────────────────────────────────────────
        public int CriadoPorId { get; set; }
        public Usuario CriadoPor { get; set; } = null!;
        public DateTime AbertaEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

        // ── Soft-delete ──────────────────────────────────────────────────────────
        /// <summary>Nulo = ativo. Preenchido = registro invisível para operação normal.</summary>
        public DateTime? DeletedAt { get; set; }
        public int? ExcluidoPorId { get; set; }
        public Usuario? ExcluidoPor { get; set; }

        // ── Navegação: Etapa 1 ───────────────────────────────────────────────────
        public Localizacao? Localizacao { get; set; }
        public ICollection<Arquivo> Arquivos { get; set; } = new List<Arquivo>();

        // ── Navegação: Etapas filhas (1:1) ───────────────────────────────────────
        public AvaliacaoRisco? AvaliacaoRisco { get; set; }
        public EncaminhamentoFinal? EncaminhamentoFinal { get; set; }

        // ── Navegação: Etapas filhas (1:N — multiplicidade por ocorrência) ────────
        public ICollection<AgendamentoVistoria> Agendamentos { get; set; } = new List<AgendamentoVistoria>();
        public ICollection<Vistoria> Vistorias { get; set; } = new List<Vistoria>();

        // ── Navegação: Etapas filhas (1:N) ───────────────────────────────────────
        public ICollection<Notificado> Notificados { get; set; } = new List<Notificado>();
        public ICollection<Observacao> Observacoes { get; set; } = new List<Observacao>();
    }
}
