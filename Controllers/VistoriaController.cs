using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Services;
using SIG_Defesa_Civil.API.Services.Vistoria;

namespace SIG_Defesa_Civil.API.Controllers
{
    [ApiController]
    [Route("api/v1/ocorrencias/{ocorrenciaId:int}")]
    [Produces("application/json")]
    public class VistoriaController : DefesaCivilBaseController
    {
        private readonly IVistoriaService _vistoriaService;
        private readonly ILogger<VistoriaController> _logger;

        public VistoriaController(
            IVistoriaService vistoriaService,
            ILogger<VistoriaController> logger)
        {
            _vistoriaService = vistoriaService;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // ETAPA 3 — AGENDAMENTO DE VISTORIA
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Agenda a vistoria designando a equipe (Etapa 3).
        /// Pré-condição: ocorrência deve estar em EM_AVALIACAO.
        /// Cria a primeira tentativa automaticamente e avança para VISTORIA_SOLICITADA.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Vistoriadores e data/hora da primeira tentativa</param>
        /// <response code="201">Agendamento criado e status atualizado para VISTORIA_SOLICITADA</response>
        /// <response code="404">Ocorrência não encontrada</response>
        /// <response code="422">Ocorrência não está em EM_AVALIACAO ou agendamento já existe</response>
        [HttpPost("agendamento")]
        [ProducesResponseType(typeof(ApiResponse<AgendamentoVistoriaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> AgendarVistoria(
            [FromRoute] int ocorrenciaId,
            [FromBody] RegistrarAgendamentoVistoriaRequest request)
        {
            try
            {
                var resultado = await _vistoriaService.AgendarAsync(
                    ocorrenciaId, request, ObterUsuarioIdInterno());

                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<AgendamentoVistoriaDto>.Success(resultado, "Vistoria agendada com sucesso"));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrada"))
            {
                return NaoEncontrado(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"AgendarVistoria(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Retorna o agendamento de vistoria de uma ocorrência.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <response code="200">Dados do agendamento com tentativas</response>
        /// <response code="404">Agendamento não encontrado para esta ocorrência</response>
        [HttpGet("agendamento")]
        [ProducesResponseType(typeof(ApiResponse<AgendamentoVistoriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterAgendamento([FromRoute] int ocorrenciaId)
        {
            try
            {
                var resultado = await _vistoriaService.ObterAgendamentoPorOcorrenciaAsync(ocorrenciaId);

                if (resultado == null)
                    return NaoEncontrado($"Agendamento não encontrado para a ocorrência {ocorrenciaId}.");

                return Ok(ApiResponse<AgendamentoVistoriaDto>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ObterAgendamento(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Adiciona uma nova tentativa de comparecimento ao agendamento.
        /// Máximo de 3 tentativas por agendamento.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="agendamentoId">ID do agendamento</param>
        /// <param name="request">Data/hora e observação da nova tentativa</param>
        /// <response code="200">Tentativa adicionada</response>
        /// <response code="404">Agendamento não encontrado</response>
        /// <response code="422">Limite de 3 tentativas atingido</response>
        [HttpPost("agendamento/{agendamentoId:int}/tentativas")]
        [ProducesResponseType(typeof(ApiResponse<AgendamentoVistoriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> AdicionarTentativa(
            [FromRoute] int ocorrenciaId,
            [FromRoute] int agendamentoId,
            [FromBody] AdicionarTentativaRequest request)
        {
            try
            {
                var resultado = await _vistoriaService.AdicionarTentativaAsync(
                    agendamentoId, request, ObterUsuarioIdInterno());

                return Ok(ApiResponse<AgendamentoVistoriaDto>.Success(
                    resultado, "Tentativa registrada com sucesso"));
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
                    $"AdicionarTentativa(ocorrencia={ocorrenciaId}, agendamento={agendamentoId})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // ETAPA 4 — VISTORIA PRESENCIAL
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Registra o resultado da vistoria presencial de campo (Etapa 4).
        /// Pré-condição: ocorrência deve estar em VISTORIA_SOLICITADA.
        /// Status avança para VISTORIA_REALIZADA.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Dados completos da vistoria de campo</param>
        /// <response code="201">Vistoria registrada e status atualizado para VISTORIA_REALIZADA</response>
        /// <response code="404">Ocorrência não encontrada</response>
        /// <response code="422">Ocorrência não está em VISTORIA_SOLICITADA ou vistoria já registrada</response>
        [HttpPost("vistoria")]
        [ProducesResponseType(typeof(ApiResponse<VistoriaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> RegistrarVistoria(
            [FromRoute] int ocorrenciaId,
            [FromBody] RegistrarVistoriaRequest request)
        {
            try
            {
                var resultado = await _vistoriaService.RegistrarVistoriaAsync(
                    ocorrenciaId, request, ObterUsuarioIdInterno());

                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<VistoriaDto>.Success(resultado, "Vistoria registrada com sucesso"));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrada"))
            {
                return NaoEncontrado(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"RegistrarVistoria(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Retorna os dados da vistoria presencial de uma ocorrência.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <response code="200">Dados completos da vistoria de campo</response>
        /// <response code="404">Vistoria não encontrada para esta ocorrência</response>
        [HttpGet("vistoria")]
        [ProducesResponseType(typeof(ApiResponse<VistoriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterVistoria([FromRoute] int ocorrenciaId)
        {
            try
            {
                var resultado = await _vistoriaService.ObterVistoriaPorOcorrenciaAsync(ocorrenciaId);

                if (resultado == null)
                    return NaoEncontrado($"Vistoria não encontrada para a ocorrência {ocorrenciaId}.");

                return Ok(ApiResponse<VistoriaDto>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ObterVistoria(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Atualiza os dados de uma vistoria presencial já registrada.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Dados atualizados da vistoria</param>
        /// <response code="200">Vistoria atualizada</response>
        /// <response code="404">Vistoria não encontrada</response>
        [HttpPut("vistoria")]
        [ProducesResponseType(typeof(ApiResponse<VistoriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AtualizarVistoria(
            [FromRoute] int ocorrenciaId,
            [FromBody] RegistrarVistoriaRequest request)
        {
            try
            {
                var resultado = await _vistoriaService.AtualizarVistoriaAsync(
                    ocorrenciaId, request, ObterUsuarioIdInterno());

                return Ok(ApiResponse<VistoriaDto>.Success(resultado, "Vistoria atualizada com sucesso"));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrada"))
            {
                return NaoEncontrado(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"AtualizarVistoria(ocorrencia={ocorrenciaId})");
            }
        }
    }
}
