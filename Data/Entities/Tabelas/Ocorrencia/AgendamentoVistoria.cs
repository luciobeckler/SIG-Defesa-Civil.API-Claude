using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Models.Tabelas
{
    /// <summary>
    /// Cabeçalho do agendamento de vistoria. Registra a dupla de vistoriadores designada.
    /// As tentativas de comparecimento ficam na entidade TentativaVistoria (1:N).
    /// Etapa 3 do fluxo. Cria este registro → status avança para VISTORIA_SOLICITADA.
    /// Uma ocorrência pode ter múltiplos agendamentos (1:N) — cada um com seu número sequencial.
    /// </summary>
    [Table("agendamentos_vistoria")]
    public class AgendamentoVistoria
    {
        public int Id { get; set; }

        // FK (dependente da ocorrência — 1:N)
        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

        // ── Sequência e estado ───────────────────────────────────────────────────
        /// <summary>Número sequencial dentro da ocorrência (1 = primeiro agendamento).</summary>
        public int Numero { get; set; } = 1;

        /// <summary>Estado do ciclo de vida do agendamento.</summary>
        public StatusAgendamento Status { get; set; } = StatusAgendamento.ATIVO;

        // ── Equipe designada ─────────────────────────────────────────────────────
        /// <summary>Vistoriador principal (obrigatório).</summary>
        public int Vistoriador1Id { get; set; }
        public Usuario Vistoriador1 { get; set; } = null!;

        /// <summary>Segundo vistoriador — duplas são o padrão operacional, mas é opcional.</summary>
        public int? Vistoriador2Id { get; set; }
        public Usuario? Vistoriador2 { get; set; }

        // ── Auditoria ────────────────────────────────────────────────────────────
        public int AgendadoPorId { get; set; }
        public Usuario AgendadoPor { get; set; } = null!;
        public DateTime AgendadoEm { get; set; } = DateTime.UtcNow;

        // ── Tentativas (1:N) ─────────────────────────────────────────────────────
        public ICollection<TentativaVistoria> Tentativas { get; set; } = new List<TentativaVistoria>();
    }
}
