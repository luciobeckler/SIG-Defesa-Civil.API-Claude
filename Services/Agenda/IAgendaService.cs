using SIG_Defesa_Civil.API.Data.DTO.Requests.Agenda;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Agenda;

namespace SIG_Defesa_Civil.API.Services.Agenda
{
    /// <summary>
    /// Visão de calendário dos agendamentos de vistoria. Módulo de leitura/organização —
    /// não interfere no ciclo de vida das ocorrências além de reposicionar agendamentos.
    /// </summary>
    public interface IAgendaService
    {
        /// <summary>
        /// Lista os agendamentos ATIVOS com data planejada dentro do intervalo [inicio, fim].
        /// </summary>
        Task<List<AgendaItemDto>> ListarPeriodoAsync(DateOnly inicio, DateOnly fim);

        /// <summary>
        /// Reposiciona um agendamento (data + turno). Mantém a tentativa mais recente
        /// sincronizada com a nova data/turno. Pré-condição: agendamento ATIVO da ocorrência.
        /// </summary>
        Task<AgendaItemDto> MoverAsync(
            int ocorrenciaId,
            int agendamentoId,
            MoverAgendamentoRequest request,
            int usuarioId);

        // ── Eventos (indisponibilidade da equipe) ────────────────────────────────

        /// <summary>
        /// Eventos que tocam o intervalo [inicio, fim] — inclusive os que começam antes
        /// ou terminam depois dele, já que a semana exibida pode cair no meio de férias.
        /// </summary>
        Task<List<EventoAgendaDto>> ListarEventosAsync(DateOnly inicio, DateOnly fim);

        Task<EventoAgendaDto> CriarEventoAsync(SalvarEventoAgendaRequest request, int usuarioId);

        Task<EventoAgendaDto> AtualizarEventoAsync(int eventoId, SalvarEventoAgendaRequest request);

        Task ExcluirEventoAsync(int eventoId);
    }
}
