using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Agenda;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Services.Agenda
{
    public class AgendaService : IAgendaService
    {
        private readonly DefesaCivilContext _context;
        private readonly ILogger<AgendaService> _logger;

        public AgendaService(DefesaCivilContext context, ILogger<AgendaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<AgendaItemDto>> ListarPeriodoAsync(DateOnly inicio, DateOnly fim)
        {
            if (fim < inicio)
                throw new InvalidOperationException("A data final não pode ser anterior à data inicial.");

            var agendamentos = await _context.AgendamentosVistoria
                .Include(a => a.Ocorrencia).ThenInclude(o => o.Localizacao)
                .Include(a => a.Ocorrencia).ThenInclude(o => o.AvaliacaoRisco)
                .Include(a => a.Vistoriador1)
                .Include(a => a.Vistoriador2)
                .Where(a => a.Status == StatusAgendamento.ATIVO
                         && a.Data != null
                         && a.Data >= inicio && a.Data <= fim
                         && a.Ocorrencia.DeletedAt == null)
                .OrderBy(a => a.Data)
                .ThenBy(a => a.Turno)
                .ToListAsync();

            return agendamentos.Select(Mapear).ToList();
        }

        public async Task<AgendaItemDto> MoverAsync(
            int ocorrenciaId,
            int agendamentoId,
            MoverAgendamentoRequest request,
            int usuarioId)
        {
            var agendamento = await _context.AgendamentosVistoria
                .Include(a => a.Ocorrencia).ThenInclude(o => o.Localizacao)
                .Include(a => a.Ocorrencia).ThenInclude(o => o.AvaliacaoRisco)
                .Include(a => a.Vistoriador1)
                .Include(a => a.Vistoriador2)
                .Include(a => a.Tentativas)
                .FirstOrDefaultAsync(a => a.Id == agendamentoId && a.OcorrenciaId == ocorrenciaId)
                ?? throw new InvalidOperationException(
                    $"Agendamento {agendamentoId} não encontrado para a ocorrência {ocorrenciaId}.");

            if (agendamento.Status != StatusAgendamento.ATIVO)
                throw new InvalidOperationException(
                    $"Só é possível mover agendamentos ATIVOS. Status atual: {agendamento.Status}.");

            agendamento.Data = request.Data;
            agendamento.Turno = request.Turno;

            // Mantém a tentativa mais recente sincronizada com a nova data/turno.
            var horaReferencia = request.Turno == TurnoVistoria.MANHA
                ? new TimeOnly(8, 0)
                : new TimeOnly(13, 0);
            var dataHora = DateTime.SpecifyKind(
                request.Data.ToDateTime(horaReferencia), DateTimeKind.Utc);

            var ultimaTentativa = agendamento.Tentativas
                .OrderByDescending(t => t.NumeroTentativa)
                .FirstOrDefault();
            if (ultimaTentativa != null)
                ultimaTentativa.DataHoraTentativa = dataHora;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Agendamento {AgendamentoId} movido para {Data} ({Turno}) pelo usuário {UsuarioId}",
                agendamentoId, request.Data, request.Turno, usuarioId);

            return Mapear(agendamento);
        }

        private static AgendaItemDto Mapear(AgendamentoVistoria a) => new()
        {
            AgendamentoId = a.Id,
            OcorrenciaId = a.OcorrenciaId,
            Protocolo = a.Ocorrencia.Protocolo,
            Bairro = a.Ocorrencia.Localizacao?.Bairro,
            Data = a.Data,
            Turno = a.Turno?.ToString(),
            Status = a.Status.ToString(),
            GrauRiscoInicial = a.Ocorrencia.AvaliacaoRisco != null
                ? a.Ocorrencia.AvaliacaoRisco.GrauRiscoInicial.ToString()
                : null,
            Vistoriador1Id = a.Vistoriador1Id,
            NomeVistoriador1 = a.Vistoriador1?.Nome,
            Vistoriador2Id = a.Vistoriador2Id,
            NomeVistoriador2 = a.Vistoriador2?.Nome,
        };
    }
}
