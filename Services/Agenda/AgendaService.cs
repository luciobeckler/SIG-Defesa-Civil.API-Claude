using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Agenda;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Agenda;
using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
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
                .Include(a => a.Vistoriador3)
                .Include(a => a.Vistoriador4)
                .Where(a => a.Status == StatusAgendamento.ATIVO
                         && a.Data != null
                         && a.Data >= inicio && a.Data <= fim
                         && a.Ocorrencia.DeletedAt == null)
                .OrderBy(a => a.Data)
                .ThenBy(a => a.Turno)
                .ToListAsync();

            return agendamentos.Select(Mapear).ToList();
        }

        // ── Eventos (indisponibilidade da equipe) ────────────────────────────────

        public async Task<List<EventoAgendaDto>> ListarEventosAsync(DateOnly inicio, DateOnly fim)
        {
            if (fim < inicio)
                throw new InvalidOperationException("A data final não pode ser anterior à data inicial.");

            // Sobreposição de intervalos: começa antes do fim da janela E termina depois
            // do começo dela. Pega férias que atravessam a semana exibida.
            var eventos = await _context.EventosAgenda
                .Include(e => e.CriadoPor)
                .Where(e => e.DataInicio <= fim && e.DataFim >= inicio)
                .OrderBy(e => e.DataInicio)
                .ThenBy(e => e.Periodo)
                .ToListAsync();

            return eventos.Select(MapearEvento).ToList();
        }

        public async Task<EventoAgendaDto> CriarEventoAsync(
            SalvarEventoAgendaRequest request, int usuarioId)
        {
            var (inicio, fim, periodo) = ValidarEvento(request);

            var evento = new EventoAgenda
            {
                Titulo = request.Titulo.Trim(),
                Observacao = string.IsNullOrWhiteSpace(request.Observacao)
                    ? null
                    : request.Observacao.Trim(),
                DataInicio = inicio,
                DataFim = fim,
                Periodo = periodo,
                CriadoPorId = usuarioId,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow,
            };

            _context.EventosAgenda.Add(evento);
            await _context.SaveChangesAsync();
            await _context.Entry(evento).Reference(e => e.CriadoPor).LoadAsync();

            _logger.LogInformation(
                "Evento de agenda criado: {Titulo} ({Inicio} a {Fim}, {Periodo})",
                evento.Titulo, evento.DataInicio, evento.DataFim, evento.Periodo);

            return MapearEvento(evento);
        }

        public async Task<EventoAgendaDto> AtualizarEventoAsync(
            int eventoId, SalvarEventoAgendaRequest request)
        {
            var evento = await _context.EventosAgenda
                .Include(e => e.CriadoPor)
                .FirstOrDefaultAsync(e => e.Id == eventoId)
                ?? throw new InvalidOperationException($"Evento {eventoId} não encontrado.");

            var (inicio, fim, periodo) = ValidarEvento(request);

            evento.Titulo = request.Titulo.Trim();
            evento.Observacao = string.IsNullOrWhiteSpace(request.Observacao)
                ? null
                : request.Observacao.Trim();
            evento.DataInicio = inicio;
            evento.DataFim = fim;
            evento.Periodo = periodo;
            evento.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapearEvento(evento);
        }

        public async Task ExcluirEventoAsync(int eventoId)
        {
            var evento = await _context.EventosAgenda.FirstOrDefaultAsync(e => e.Id == eventoId)
                ?? throw new InvalidOperationException($"Evento {eventoId} não encontrado.");

            // Some de vez: evento é anotação de agenda, não tem histórico a preservar.
            _context.EventosAgenda.Remove(evento);
            await _context.SaveChangesAsync();
        }

        private static (DateOnly inicio, DateOnly fim, PeriodoEventoAgenda periodo) ValidarEvento(
            SalvarEventoAgendaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Titulo))
                throw new InvalidOperationException("Informe um título para o evento.");

            if (request.DataFim < request.DataInicio)
                throw new InvalidOperationException(
                    "A data final não pode ser anterior à data inicial.");

            if (!Enum.TryParse<PeriodoEventoAgenda>(request.Periodo, ignoreCase: true, out var periodo))
                throw new InvalidOperationException(
                    "Período inválido. Use MANHA, TARDE ou DIA_TODO.");

            return (request.DataInicio, request.DataFim, periodo);
        }

        private static EventoAgendaDto MapearEvento(EventoAgenda e) => new()
        {
            Id = e.Id,
            Titulo = e.Titulo,
            Observacao = e.Observacao,
            DataInicio = e.DataInicio,
            DataFim = e.DataFim,
            Periodo = e.Periodo.ToString(),
            CriadoPor = e.CriadoPor?.Nome,
            CriadoEm = e.CriadoEm,
        };

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
                .Include(a => a.Vistoriador3)
                .Include(a => a.Vistoriador4)
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
            Vistoriador3Id = a.Vistoriador3Id,
            NomeVistoriador3 = a.Vistoriador3?.Nome,
            Vistoriador4Id = a.Vistoriador4Id,
            NomeVistoriador4 = a.Vistoriador4?.Nome,
        };
    }
}
