using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Agenda
{
    /// <summary>Criação e edição de um evento da agenda. Usado por POST e PATCH.</summary>
    public class SalvarEventoAgendaRequest
    {
        [Required(ErrorMessage = "Informe um título para o evento.")]
        [StringLength(120, MinimumLength = 2)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Observacao { get; set; }

        [Required] public DateOnly DataInicio { get; set; }

        /// <summary>Último dia coberto. Para evento de um dia só, repita a data inicial.</summary>
        [Required] public DateOnly DataFim { get; set; }

        /// <summary>MANHA, TARDE ou DIA_TODO.</summary>
        [Required] public string Periodo { get; set; } = "DIA_TODO";
    }
}
