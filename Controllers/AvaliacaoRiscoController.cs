using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Services;
using SIG_Defesa_Civil.API.Services.AvaliacaoRisco;

namespace SIG_Defesa_Civil.API.Controllers
{
    [ApiController]
    [Route("api/v1/ocorrencias/{ocorrenciaId:int}/avaliacao-risco")]
    [Produces("application/json")]
    public class AvaliacaoRiscoController : DefesaCivilBaseController
    {
        private readonly IAvaliacaoRiscoService _avaliacaoRiscoService;
        private readonly ILogger<AvaliacaoRiscoController> _logger;

        public AvaliacaoRiscoController(
            IAvaliacaoRiscoService avaliacaoRiscoService,
            ILogger<AvaliacaoRiscoController> logger)
        {
            _avaliacaoRiscoService = avaliacaoRiscoService;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // POST — Etapa 2: Registrar avaliação de risco
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Registra a avaliação inicial de risco de uma ocorrência (Etapa 2).
        /// Pré-condição: ocorrência deve estar com status ABERTA.
        /// Ao registrar, o status avança para EM_AVALIACAO.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Dados da avaliação de risco</param>
        /// <response code="201">Avaliação registrada e status atualizado para EM_AVALIACAO</response>
        /// <response code="404">Ocorrência não encontrada</response>
        /// <response code="422">Ocorrência não está em status ABERTA, ou já possui avaliação</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AvaliacaoRiscoDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> RegistrarAvaliacaoRisco(
            [FromRoute] int ocorrenciaId,
            [FromBody] RegistrarAvaliacaoRiscoRequest request)
        {
            try
            {
                var resultado = await _avaliacaoRiscoService.RegistrarAsync(
                    ocorrenciaId, request, ObterUsuarioIdInterno());

                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<AvaliacaoRiscoDto>.Success(resultado, "Avaliação de risco registrada com sucesso"));
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
                return ErroInterno(ex, _logger, $"RegistrarAvaliacaoRisco(ocorrencia={ocorrenciaId})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // GET — Consultar avaliação de risco
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna a avaliação de risco de uma ocorrência.
        /// Retorna 404 se a Etapa 2 ainda não foi preenchida.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <response code="200">Dados da avaliação de risco</response>
        /// <response code="404">Avaliação não encontrada para esta ocorrência</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<AvaliacaoRiscoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterAvaliacaoRisco([FromRoute] int ocorrenciaId)
        {
            try
            {
                var resultado = await _avaliacaoRiscoService.ObterPorOcorrenciaAsync(ocorrenciaId);

                if (resultado == null)
                    return NaoEncontrado($"Avaliação de risco não encontrada para a ocorrência {ocorrenciaId}.");

                return Ok(ApiResponse<AvaliacaoRiscoDto>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ObterAvaliacaoRisco(ocorrencia={ocorrenciaId})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // PUT — Atualizar avaliação de risco
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Atualiza a avaliação de risco existente de uma ocorrência.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Novos dados da avaliação</param>
        /// <response code="200">Avaliação atualizada</response>
        /// <response code="404">Avaliação não encontrada</response>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<AvaliacaoRiscoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AtualizarAvaliacaoRisco(
            [FromRoute] int ocorrenciaId,
            [FromBody] RegistrarAvaliacaoRiscoRequest request)
        {
            try
            {
                var resultado = await _avaliacaoRiscoService.AtualizarAsync(
                    ocorrenciaId, request, ObterUsuarioIdInterno());

                return Ok(ApiResponse<AvaliacaoRiscoDto>.Success(resultado, "Avaliação de risco atualizada"));
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
                return ErroInterno(ex, _logger, $"AtualizarAvaliacaoRisco(ocorrencia={ocorrenciaId})");
            }
        }
    }
}
