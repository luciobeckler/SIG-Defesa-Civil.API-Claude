using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Services;
using SIG_Defesa_Civil.API.Services.Encaminhamento;

namespace SIG_Defesa_Civil.API.Controllers
{
    [ApiController]
    [Route("api/v1/ocorrencias/{ocorrenciaId:int}/encaminhamento")]
    [Produces("application/json")]
    public class EncaminhamentoController : DefesaCivilBaseController
    {
        private readonly IEncaminhamentoService _encaminhamentoService;
        private readonly ILogger<EncaminhamentoController> _logger;

        public EncaminhamentoController(
            IEncaminhamentoService encaminhamentoService,
            ILogger<EncaminhamentoController> logger)
        {
            _encaminhamentoService = encaminhamentoService;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // ETAPA 6 — ENCAMINHAMENTO FINAL E ENCERRAMENTO
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Registra os encaminhamentos formais e encerra a ocorrência (Etapa 6).
        /// Pré-condição: ocorrência deve estar em NOTIFICADA.
        /// Status avança para ENCERRADA.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Encaminhamentos, canal de entrega do relatório</param>
        /// <response code="201">Encaminhamento registrado e ocorrência encerrada</response>
        /// <response code="404">Ocorrência não encontrada</response>
        /// <response code="422">Ocorrência não está em NOTIFICADA ou encaminhamento já existe</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<EncaminhamentoFinalDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> RegistrarEncaminhamento(
            [FromRoute] int ocorrenciaId,
            [FromBody] RegistrarEncaminhamentoRequest request)
        {
            try
            {
                var resultado = await _encaminhamentoService.RegistrarAsync(
                    ocorrenciaId, request, ObterUsuarioIdInterno());

                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<EncaminhamentoFinalDto>.Success(
                        resultado, "Encaminhamento registrado e ocorrência encerrada"));
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
                return ErroInterno(ex, _logger, $"RegistrarEncaminhamento(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Retorna o encaminhamento final de uma ocorrência.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <response code="200">Dados do encaminhamento final</response>
        /// <response code="404">Encaminhamento não encontrado para esta ocorrência</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<EncaminhamentoFinalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterEncaminhamento([FromRoute] int ocorrenciaId)
        {
            try
            {
                var resultado = await _encaminhamentoService.ObterPorOcorrenciaAsync(ocorrenciaId);

                if (resultado == null)
                    return NaoEncontrado($"Encaminhamento não encontrado para a ocorrência {ocorrenciaId}.");

                return Ok(ApiResponse<EncaminhamentoFinalDto>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ObterEncaminhamento(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Atualiza o encaminhamento final (ex: vincular relatório após upload, ajustar texto).
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Dados atualizados do encaminhamento</param>
        /// <response code="200">Encaminhamento atualizado</response>
        /// <response code="404">Encaminhamento não encontrado</response>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<EncaminhamentoFinalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AtualizarEncaminhamento(
            [FromRoute] int ocorrenciaId,
            [FromBody] RegistrarEncaminhamentoRequest request)
        {
            try
            {
                var resultado = await _encaminhamentoService.AtualizarAsync(
                    ocorrenciaId, request, ObterUsuarioIdInterno());

                return Ok(ApiResponse<EncaminhamentoFinalDto>.Success(
                    resultado, "Encaminhamento atualizado com sucesso"));
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
                return ErroInterno(ex, _logger, $"AtualizarEncaminhamento(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Reabre uma ocorrência encerrada, revertendo o status para NOTIFICADA.
        /// Use quando for necessário corrigir o encaminhamento ou adicionar notificados.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="motivo">Motivo obrigatório da reabertura</param>
        /// <response code="200">Ocorrência reaberta com status NOTIFICADA</response>
        /// <response code="404">Ocorrência não encontrada ou não está ENCERRADA</response>
        [HttpPost("reabrir")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ReabrirOcorrencia(
            [FromRoute] int ocorrenciaId,
            [FromQuery] string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                return BadRequest(ApiResponse<object>.Error(
                    "O motivo da reabertura é obrigatório.",
                    ErrosRequisicoes.DADOS_INVALIDOS));

            try
            {
                await _encaminhamentoService.ReabrirAsync(ocorrenciaId, ObterUsuarioIdInterno(), motivo);

                return Ok(ApiResponse<object>.Success(
                    (object?)null, "Ocorrência reaberta com sucesso. Status revertido para NOTIFICADA."));
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
                return ErroInterno(ex, _logger, $"ReabrirOcorrencia(ocorrencia={ocorrenciaId})");
            }
        }
    }
}
