using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Exceptions;
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
        /// Cria um novo agendamento de vistoria (Etapa 3).
        /// Pré-condição: ocorrência deve estar em EM_AVALIACAO (1º agendamento)
        /// ou VISTORIA_SOLICITADA (re-agendamento).
        /// Cria a primeira tentativa automaticamente.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Vistoriadores e data/hora da primeira tentativa</param>
        /// <response code="201">Agendamento criado</response>
        /// <response code="404">Ocorrência não encontrada</response>
        /// <response code="422">Status inválido ou vistoriadores inválidos</response>
        [HttpPost("agendamentos")]
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
        /// Lista todos os agendamentos de uma ocorrência em ordem crescente de número.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <response code="200">Lista de agendamentos (pode ser vazia)</response>
        [HttpGet("agendamentos")]
        [ProducesResponseType(typeof(ApiResponse<List<AgendamentoVistoriaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarAgendamentos([FromRoute] int ocorrenciaId)
        {
            try
            {
                var resultado = await _vistoriaService.ListarAgendamentosAsync(ocorrenciaId);
                return Ok(ApiResponse<List<AgendamentoVistoriaDto>>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ListarAgendamentos(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Retorna um agendamento específico pelo ID.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="agendamentoId">ID do agendamento</param>
        /// <response code="200">Dados do agendamento com tentativas</response>
        /// <response code="404">Agendamento não encontrado</response>
        [HttpGet("agendamentos/{agendamentoId:int}")]
        [ProducesResponseType(typeof(ApiResponse<AgendamentoVistoriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterAgendamento(
            [FromRoute] int ocorrenciaId,
            [FromRoute] int agendamentoId)
        {
            try
            {
                var resultado = await _vistoriaService.ObterAgendamentoPorIdAsync(agendamentoId);

                if (resultado == null)
                    return NaoEncontrado($"Agendamento {agendamentoId} não encontrado.");

                return Ok(ApiResponse<AgendamentoVistoriaDto>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger,
                    $"ObterAgendamento(ocorrencia={ocorrenciaId}, agendamento={agendamentoId})");
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
        /// <response code="422">Limite de tentativas ou status inválido</response>
        [HttpPost("agendamentos/{agendamentoId:int}/tentativas")]
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
        /// <response code="201">Vistoria registrada</response>
        /// <response code="404">Ocorrência não encontrada</response>
        /// <response code="422">Ocorrência não está em VISTORIA_SOLICITADA</response>
        [HttpPost("vistorias")]
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
        /// Lista todas as vistorias de uma ocorrência em ordem crescente de número.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <response code="200">Lista de vistorias (pode ser vazia)</response>
        [HttpGet("vistorias")]
        [ProducesResponseType(typeof(ApiResponse<List<VistoriaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarVistorias([FromRoute] int ocorrenciaId)
        {
            try
            {
                var resultado = await _vistoriaService.ListarVistoriasAsync(ocorrenciaId);
                return Ok(ApiResponse<List<VistoriaDto>>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ListarVistorias(ocorrencia={ocorrenciaId})");
            }
        }

        /// <summary>
        /// Retorna uma vistoria específica pelo ID.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="vistoriaId">ID da vistoria</param>
        /// <response code="200">Dados da vistoria</response>
        /// <response code="404">Vistoria não encontrada</response>
        [HttpGet("vistorias/{vistoriaId:int}")]
        [ProducesResponseType(typeof(ApiResponse<VistoriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterVistoria(
            [FromRoute] int ocorrenciaId,
            [FromRoute] int vistoriaId)
        {
            try
            {
                var resultado = await _vistoriaService.ObterVistoriaPorIdAsync(vistoriaId);

                if (resultado == null)
                    return NaoEncontrado($"Vistoria {vistoriaId} não encontrada.");

                return Ok(ApiResponse<VistoriaDto>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger,
                    $"ObterVistoria(ocorrencia={ocorrenciaId}, vistoria={vistoriaId})");
            }
        }

        /// <summary>
        /// Atualiza os dados de uma vistoria presencial já registrada.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="vistoriaId">ID da vistoria a atualizar</param>
        /// <param name="request">Dados atualizados da vistoria</param>
        /// <response code="200">Vistoria atualizada</response>
        /// <response code="404">Vistoria não encontrada</response>
        [HttpPut("vistorias/{vistoriaId:int}")]
        [ProducesResponseType(typeof(ApiResponse<VistoriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AtualizarVistoria(
            [FromRoute] int ocorrenciaId,
            [FromRoute] int vistoriaId,
            [FromBody] RegistrarVistoriaRequest request)
        {
            try
            {
                var resultado = await _vistoriaService.AtualizarVistoriaPorIdAsync(
                    vistoriaId, request, ObterUsuarioIdInterno());

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
                return ErroInterno(ex, _logger,
                    $"AtualizarVistoria(ocorrencia={ocorrenciaId}, vistoria={vistoriaId})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // FOTOS DE CAMPO — POST .../vistorias/{vistoriaId}/fotos
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Adiciona fotos de campo (FOTO_CAMPO) a uma vistoria já registrada.
        /// Arquivos salvos em [Protocolo]/Fotos/Fotos_da_Vistoria/ (CA02).
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="vistoriaId">ID da vistoria</param>
        /// <param name="fotos">Lista de imagens tiradas em campo</param>
        /// <response code="201">Fotos adicionadas com sucesso</response>
        /// <response code="400">Nenhuma foto enviada ou arquivo muito grande</response>
        /// <response code="404">Vistoria não encontrada para esta ocorrência</response>
        /// <response code="503">Falha no armazenamento</response>
        [HttpPost("vistorias/{vistoriaId:int}/fotos")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> AdicionarFotosCampo(
            [FromRoute] int ocorrenciaId,
            [FromRoute] int vistoriaId,
            [FromForm] List<IFormFile>? fotos)
        {
            try
            {
                if (fotos == null || fotos.Count == 0)
                    return BadRequest(ApiResponse<object>.Error(
                        "É obrigatório enviar ao menos uma foto de campo",
                        ErrosRequisicoes.ARQUIVOS_AUSENTES));

                const long maxFileSize = 10 * 1024 * 1024; // 10 MB
                var arquivoGrande = fotos.FirstOrDefault(f => f.Length > maxFileSize);
                if (arquivoGrande != null)
                    return BadRequest(ApiResponse<object>.Error(
                        $"Arquivo '{arquivoGrande.FileName}' excede o tamanho máximo de 10MB",
                        ErrosRequisicoes.ARQUIVO_MUITO_GRANDE));

                var totalSalvos = await _vistoriaService.AdicionarFotosCampoAsync(
                    ocorrenciaId, vistoriaId, fotos, ObterUsuarioIdInterno());

                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<object>.Success(
                        null,
                        $"{totalSalvos} foto(s) de campo adicionadas com sucesso"));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrada"))
            {
                return NaoEncontrado(ex.Message);
            }
            catch (StorageException ex)
            {
                _logger.LogError(ex, "Falha ao salvar fotos de campo. Vistoria: {VistoriaId}", vistoriaId);
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Error(
                        "Sistema temporariamente indisponível. Tente novamente em alguns minutos.",
                        ErrosRequisicoes.UPLOAD_FAILED));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger,
                    $"AdicionarFotosCampo(ocorrencia={ocorrenciaId}, vistoria={vistoriaId})");
            }
        }
    }
}
