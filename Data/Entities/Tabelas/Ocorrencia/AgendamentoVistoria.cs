using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Models.Tabelas
{
    /// <summary>
    /// Cabeçalho do agendamento de vistoria. Registra a dupla de vistoriadores designada.
    /// As tentativas de comparecimento ficam na entidade TentativaVistoria (1:N).
    /// Etapa 3 do fluxo. Cria este registro → status avança para VISTORIA_SOLICITADA.
    /// </summary>
    [Table("agendamentos_vistoria")]
    public class AgendamentoVistoria
    {
        public int Id { get; set; }

        // FK (dependente da ocorrência — 1:1)
        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

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
