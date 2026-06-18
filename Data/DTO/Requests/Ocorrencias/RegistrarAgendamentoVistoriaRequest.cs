using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Agendamento de vistoria — Etapa 3.
    /// Registra apenas data e turno. Os vistoriadores são designados ao registrar a vistoria (Etapa 4).
    /// Ao registrar, o status avança para VISTORIA_SOLICITADA.
    /// </summary>
    public class RegistrarAgendamentoVistoriaRequest
    {
        /// <summary>Data prevista para a visita.</summary>
        [Required] public DateOnly Data { get; set; }

        /// <summary>Turno preferencial da visita (Manhã ou Tarde).</summary>
        [Required] public TurnoVistoria Turno { get; set; }

        /// <summary>Observação opcional sobre o agendamento.</summary>
        public string? Observacao { get; set; }
    }

    /// <summary>
    /// Registro de uma tentativa adicional de comparecimento dentro de um agendamento.
    /// </summary>
    public class AdicionarTentativaRequest
    {
        [Required] public DateTime DataHoraTentativa { get; set; }

        /// <summary>Motivo da não realização na tentativa anterior (ex: "Morador ausente").</summary>
        public string? Observacao { get; set; }
    }
}
