using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Services;
using SIG_Defesa_Civil.API.Services.Notificacao;

namespace SIG_Defesa_Civil.API.Controllers
{
    [ApiController]
    [Route("api/v1/ocorrencias/{ocorrenciaId:int}")]
    [Produces("application/json")]
    public class NotificacaoController : DefesaCivilBaseController
    {
        private readonly INotificacaoService _notificacaoService;
        private readonly ILogger<NotificacaoController> _logger;

        public NotificacaoController(
            INotificacaoService notificacaoService,
            ILogger<NotificacaoController> logger)
        {
            _notificacaoService = notificacaoService;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // ETAPA 5 — NOTIFICAÇÃO DE MORADORES
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Registra os moradores/responsáveis notificados (Etapa 5).
        /// Pré-condição: ocorrência deve estar em VISTORIA_REALIZADA ou NOTIFICADA.
        /// Ao registrar o primeiro notificado, o status avança para NOTIFICADA.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Lista de notificados (nome, RG/CPF, data)</param>
        /// <response code="201">Notificados registrados</response>
        /// <response code="404">Ocorrência não encontrada</response>
        /// <response code="422">Ocorrência não está em VISTORIA_REALIZADA ou NOTIFICADA</response>
        [HttpPost("notificados")]
        [ProducesResponseType(typeof(ApiResponse<List<NotificadoDto>>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> RegistrarNotificados(
            [FromRoute] int ocorrenciaId,
            [FromBody] RegistrarNotificadosRequest request)
        {
            try
            {
                if (request.Notificados == null || request.Notificados.Count == 0)
                    return BadRequest(ApiResponse<object>.Error(
                        "A lista de notificados não pode ser vazia.",
                        ErrosRequisicoes.DADOS_INVALIDOS));

                var resultado = await _notificacaoService.RegistrarAsync(
                    ocorrenciaId, request, ObterUsuarioIdInterno());

                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<List<NotificadoDto>>.Success(
                        resultado,
                        $"{resultado.Count} notificado(s) registrado(s) com sucesso"));
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
                return ErroInterno(ex, _logger, $"RegistrarNotificados(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Lista todos os notificados de uma ocorrência.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <response code="200">Lista de notificados ordenada por data de registro</response>
        [HttpGet("notificados")]
        [ProducesResponseType(typeof(ApiResponse<List<NotificadoDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarNotificados([FromRoute] int ocorrenciaId)
        {
            try
            {
                var resultado = await _notificacaoService.ListarPorOcorrenciaAsync(ocorrenciaId);

                return Ok(ApiResponse<List<NotificadoDto>>.Success(
                    resultado, $"{resultado.Count} notificado(s) encontrado(s)"));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ListarNotificados(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Remove um notificado pelo seu ID.
        /// Se for o último notificado, o status da ocorrência reverte para VISTORIA_REALIZADA.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="notificadoId">ID do notificado a remover</param>
        /// <response code="204">Notificado removido</response>
        /// <response code="404">Notificado não encontrado</response>
        [HttpDelete("notificados/{notificadoId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoverNotificado(
            [FromRoute] int ocorrenciaId,
            [FromRoute] int notificadoId)
        {
            try
            {
                await _notificacaoService.RemoverNotificadoAsync(notificadoId, ObterUsuarioIdInterno());
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger,
                    $"RemoverNotificado(ocorrencia={ocorrenciaId}, notificado={notificadoId})");
            }
        }

        /// <summary>
        /// Salva a assinatura do notificado (PNG do canvas) — obrigatória quando o
        /// recebimento do relatório foi PRESENCIAL. Substitui assinatura anterior.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="notificadoId">ID do notificado</param>
        /// <param name="arquivos">Imagem PNG da assinatura (max 2 MB)</param>
        /// <response code="201">Assinatura salva</response>
        /// <response code="404">Notificado não encontrado</response>
        [HttpPost("notificados/{notificadoId:int}/assinatura")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SalvarAssinaturaNotificado(
            [FromRoute] int ocorrenciaId,
            [FromRoute] int notificadoId,
            [FromForm] List<IFormFile>? arquivos)
        {
            var arquivo = arquivos?.FirstOrDefault();
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                    return BadRequest(ApiResponse<object>.Error(
                        "Nenhuma assinatura enviada.",
                        ErrosRequisicoes.ARQUIVOS_AUSENTES));

                const long maxFileSize = 2 * 1024 * 1024;
                if (arquivo.Length > maxFileSize)
                    return BadRequest(ApiResponse<object>.Error(
                        "Arquivo de assinatura excede o tamanho máximo de 2 MB.",
                        ErrosRequisicoes.ARQUIVO_MUITO_GRANDE));

                await _notificacaoService.SalvarAssinaturaNotificadoAsync(
                    ocorrenciaId, notificadoId, arquivo, ObterUsuarioIdInterno());

                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<object>.Success(null, "Assinatura do notificado salva com sucesso"));
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger,
                    $"SalvarAssinaturaNotificado(ocorrencia={ocorrenciaId}, notificado={notificadoId})");
            }
        }
    }
}
