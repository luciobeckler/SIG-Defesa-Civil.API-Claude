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
    }
}
