namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Agenda
{
    /// <summary>
    /// Card do calendário de agendamentos — um agendamento ATIVO posicionado em um dia/turno.
    /// Achata os dados da ocorrência e da equipe designada para exibição direta.
    /// </summary>
    public class AgendaItemDto
    {
        public int AgendamentoId { get; set; }
        public int OcorrenciaId { get; set; }
        public string Protocolo { get; set; } = string.Empty;
        public string? Bairro { get; set; }

        /// <summary>Data planejada da visita (YYYY-MM-DD).</summary>
        public DateOnly? Data { get; set; }

        /// <summary>Turno planejado (MANHA / TARDE).</summary>
        public string? Turno { get; set; }

        public string Status { get; set; } = string.Empty;

        /// <summary>Grau de risco inicial — útil para colorir o card.</summary>
        public string? GrauRiscoInicial { get; set; }

        // ── Equipe designada ──────────────────────────────────────────────────────
        public int? Vistoriador1Id { get; set; }
        public string? NomeVistoriador1 { get; set; }
        public int? Vistoriador2Id { get; set; }
        public string? NomeVistoriador2 { get; set; }
        public int? Vistoriador3Id { get; set; }
        public string? NomeVistoriador3 { get; set; }
        public int? Vistoriador4Id { get; set; }
        public string? NomeVistoriador4 { get; set; }
    }
}
