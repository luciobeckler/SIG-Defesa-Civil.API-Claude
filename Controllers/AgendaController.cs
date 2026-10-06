using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Agenda;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Agenda;
using SIG_Defesa_Civil.API.Services;
using SIG_Defesa_Civil.API.Services.Agenda;

namespace SIG_Defesa_Civil.API.Controllers
{
    /// <summary>
    /// Calendário de agendamentos de vistoria. Visão de organização semanal:
    /// lista os agendamentos por dia/turno e permite reposicioná-los.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1")]
    [Produces("application/json")]
    public class AgendaController : DefesaCivilBaseController
    {
        private readonly IAgendaService _agendaService;
        private readonly ILogger<AgendaController> _logger;

        public AgendaController(IAgendaService agendaService, ILogger<AgendaController> logger)
        {
            _agendaService = agendaService;
            _logger = logger;
        }

        /// <summary>
        /// Lista os agendamentos ATIVOS com data planejada no intervalo informado.
        /// </summary>
        /// <param name="inicio">Data inicial do período (YYYY-MM-DD)</param>
        /// <param name="fim">Data final do período (YYYY-MM-DD)</param>
        /// <response code="200">Lista de cards do calendário (pode ser vazia)</response>
        /// <response code="422">Intervalo de datas inválido</response>
        [HttpGet("agenda")]
        [ProducesResponseType(typeof(ApiResponse<List<AgendaItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ListarAgenda(
            [FromQuery] DateOnly inicio,
            [FromQuery] DateOnly fim)
        {
            try
            {
                var resultado = await _agendaService.ListarPeriodoAsync(inicio, fim);
                return Ok(ApiResponse<List<AgendaItemDto>>.Success(resultado));
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ListarAgenda(inicio={inicio}, fim={fim})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // EVENTOS DA AGENDA — indisponibilidade da equipe
        // ══════════════════════════════════════════════════════════════════════════
        // Um evento não bloqueia o agendamento: a tela avisa e deixa seguir, porque
        // emergência não pode ficar travada por um compromisso marcado semanas antes.

        /// <summary>
        /// Lista os eventos que tocam o intervalo informado — inclusive os que começam
        /// antes ou terminam depois dele.
        /// </summary>
        /// <param name="inicio">Data inicial do período (YYYY-MM-DD)</param>
        /// <param name="fim">Data final do período (YYYY-MM-DD)</param>
        /// <response code="200">Lista de eventos (pode ser vazia)</response>
        /// <response code="422">Intervalo de datas inválido</response>
        [HttpGet("agenda/eventos")]
        [ProducesResponseType(typeof(ApiResponse<List<EventoAgendaDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ListarEventos(
            [FromQuery] DateOnly inicio,
            [FromQuery] DateOnly fim)
        {
            try
            {
                var eventos = await _agendaService.ListarEventosAsync(inicio, fim);
                return Ok(ApiResponse<List<EventoAgendaDto>>.Success(eventos));
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ListarEventos(inicio={inicio}, fim={fim})");
            }
        }

        /// <summary>Cadastra um evento que ocupa a agenda da equipe.</summary>
        /// <response code="201">Evento criado</response>
        /// <response code="422">Dados inválidos (título vazio, período desconhecido, fim antes do início)</response>
        [HttpPost("agenda/eventos")]
        [ProducesResponseType(typeof(ApiResponse<EventoAgendaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CriarEvento([FromBody] SalvarEventoAgendaRequest request)
        {
            try
            {
                var evento = await _agendaService.CriarEventoAsync(request, ObterUsuarioIdInterno());
                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<EventoAgendaDto>.Success(evento, "Evento cadastrado na agenda."));
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, "CriarEvento");
            }
        }

        /// <summary>Edita um evento da agenda.</summary>
        /// <response code="200">Evento atualizado</response>
        /// <response code="404">Evento não encontrado</response>
        /// <response code="422">Dados inválidos</response>
        [HttpPut("agenda/eventos/{eventoId:int}")]
        [ProducesResponseType(typeof(ApiResponse<EventoAgendaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> AtualizarEvento(
            [FromRoute] int eventoId,
            [FromBody] SalvarEventoAgendaRequest request)
        {
            try
            {
                var evento = await _agendaService.AtualizarEventoAsync(eventoId, request);
                return Ok(ApiResponse<EventoAgendaDto>.Success(evento, "Evento atualizado."));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrado"))
            {
                return NaoEncontrado(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"AtualizarEvento({eventoId})");
            }
        }

        /// <summary>Remove um evento da agenda.</summary>
        /// <response code="204">Evento removido</response>
        /// <response code="404">Evento não encontrado</response>
        [HttpDelete("agenda/eventos/{eventoId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExcluirEvento([FromRoute] int eventoId)
        {
            try
            {
                await _agendaService.ExcluirEventoAsync(eventoId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ExcluirEvento({eventoId})");
            }
        }

        /// <summary>
        /// Reposiciona um agendamento no calendário (arrastar-e-soltar): atualiza data e turno.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência dona do agendamento</param>
        /// <param name="agendamentoId">ID do agendamento</param>
        /// <param name="request">Nova data e turno</param>
        /// <response code="200">Agendamento reposicionado</response>
        /// <response code="404">Agendamento não encontrado</response>
        /// <response code="422">Status inválido ou dados inválidos</response>
        [HttpPatch("ocorrencias/{ocorrenciaId:int}/agendamentos/{agendamentoId:int}/agenda")]
        [ProducesResponseType(typeof(ApiResponse<AgendaItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> MoverAgendamento(
            [FromRoute] int ocorrenciaId,
            [FromRoute] int agendamentoId,
            [FromBody] MoverAgendamentoRequest request)
        {
            try
            {
                var resultado = await _agendaService.MoverAsync(
                    ocorrenciaId, agendamentoId, request, ObterUsuarioIdInterno());

                return Ok(ApiResponse<AgendaItemDto>.Success(resultado, "Agendamento reposicionado"));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrado"))
            {
                return NaoEncontrado(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger,
                    $"MoverAgendamento(ocorrencia={ocorrenciaId}, agendamento={agendamentoId})");
            }
        }
    }
}
