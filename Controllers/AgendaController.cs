using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
