namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Agenda
{
    /// <summary>
    /// Evento que ocupa a agenda da equipe — feriado, treinamento, operação de chuva.
    /// Aparece como faixa no calendário e marca o período como desaconselhado para vistoria.
    /// </summary>
    public class EventoAgendaDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Observacao { get; set; }

        /// <summary>Primeiro dia coberto (YYYY-MM-DD).</summary>
        public DateOnly DataInicio { get; set; }

        /// <summary>Último dia coberto, inclusive (YYYY-MM-DD).</summary>
        public DateOnly DataFim { get; set; }

        /// <summary>MANHA, TARDE ou DIA_TODO — vale igual em todos os dias do intervalo.</summary>
        public string Periodo { get; set; } = string.Empty;

        public string? CriadoPor { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
