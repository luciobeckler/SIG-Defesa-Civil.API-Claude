using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Reposiciona um agendamento no calendário (arrastar-e-soltar):
    /// atualiza a data planejada e o turno.
    /// </summary>
    public class MoverAgendamentoRequest
    {
        /// <summary>Nova data planejada da visita.</summary>
        [Required] public DateOnly Data { get; set; }

        /// <summary>Novo turno (Manhã ou Tarde).</summary>
        [Required] public TurnoVistoria Turno { get; set; }
    }
}
