using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Compromisso que ocupa a agenda da equipe e desaconselha marcar vistoria no
    /// período — feriado, treinamento, mutirão, operação de chuva.
    ///
    /// Não é agendamento nem ocorrência: só sinaliza indisponibilidade. O sistema
    /// <b>avisa</b> ao marcar uma vistoria em cima de um evento, mas não impede:
    /// emergência não pode ficar travada por um compromisso cadastrado semanas antes.
    ///
    /// Vale para a equipe toda. Ausência de uma pessoa específica seria outro
    /// desenho (evento por vistoriador) e não está contemplada aqui.
    /// </summary>
    [Table("eventos_agenda")]
    public class EventoAgenda
    {
        public int Id { get; set; }

        /// <summary>O que aparece na faixa do calendário (ex.: "Feriado municipal").</summary>
        public string Titulo { get; set; } = null!;

        /// <summary>Detalhe opcional, exibido ao abrir o evento.</summary>
        public string? Observacao { get; set; }

        /// <summary>
        /// Primeiro dia coberto. Evento de um dia só tem início e fim iguais.
        /// </summary>
        public DateOnly DataInicio { get; set; }

        /// <summary>Último dia coberto (inclusive).</summary>
        public DateOnly DataFim { get; set; }

        /// <summary>
        /// Período ocupado — vale igual para todos os dias do intervalo. Férias de
        /// uma semana é um evento DIA_TODO de segunda a sexta, não cinco registros.
        /// </summary>
        public PeriodoEventoAgenda Periodo { get; set; } = PeriodoEventoAgenda.DIA_TODO;

        // ── Auditoria ────────────────────────────────────────────────────────────
        public int CriadoPorId { get; set; }
        public Usuario CriadoPor { get; set; } = null!;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }
}
