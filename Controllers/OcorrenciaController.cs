using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Exceptions;
using SIG_Defesa_Civil.API.Services;
using SIG_Defesa_Civil.API.Services.Ocorrencia;
using SIG_Defesa_Civil.API.Services.Storage;
using System.Text.Json;

namespace SIG_Defesa_Civil.API.Controllers
{
    [ApiController]
    [Route("api/v1/ocorrencias")]
    [Produces("application/json")]
    public class OcorrenciaController : DefesaCivilBaseController
    {
        private readonly IOcorrenciaService _ocorrenciaService;
        private readonly IStorageService _storageService;
        private readonly ILogger<OcorrenciaController> _logger;

        public OcorrenciaController(
            IOcorrenciaService ocorrenciaService,
            IStorageService storageService,
            ILogger<OcorrenciaController> logger)
        {
            _ocorrenciaService = ocorrenciaService;
            _storageService = storageService;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // POST /api/v1/ocorrencias — Etapa 1: Criar
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Cria uma nova ocorrência de Defesa Civil (Etapa 1).
        /// </summary>
        /// <param name="dados">JSON com dados estruturados (cidadão, local, descrição)</param>
        /// <param name="comprovante">Comprovante de residência (obrigatório) → salvo em Documentos/</param>
        /// <param name="fotos">Fotos do local tiradas pelo cidadão (obrigatório) → salvas em Fotos/Fotos_do_Municipe/</param>
        /// <returns>Protocolo gerado e dados da ocorrência criada</returns>
        /// <response code="201">Ocorrência criada com sucesso</response>
        /// <response code="400">Dados inválidos ou ausentes</response>
        /// <response code="503">Sistema temporariamente indisponível (falha no armazenamento)</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<OcorrenciaCriadaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CriarOcorrencia([FromForm] CriarOcorrenciaUploadDto upload)
        {
            // Restaure as variáveis para o seu código atual continuar funcionando sem precisar reescrever tudo abaixo:
            var dados = upload.Dados;
            var comprovante = upload.Comprovante;
            var fotos = upload.Fotos;

            var ipOrigem = ObterIpCliente();
            // Endpoint público — cidadãos não têm conta no sistema.
            // O ID do criador é resolvido internamente pelo serviço a partir do CPF informado.

            _logger.LogInformation(
                "Recebida requisição de criação de ocorrência. IP: {IP}, Fotos: {Count}",
                ipOrigem, fotos?.Count ?? 0);

            try
            {
                if (string.IsNullOrWhiteSpace(dados))
                    return BadRequest(ApiResponse<object>.Error(
                        "O campo 'dados' é obrigatório",
                        ErrosRequisicoes.DADOS_AUSENTES));

                CriarOcorrenciaRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<CriarOcorrenciaRequest>(
                        dados,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Erro ao deserializar JSON de dados");
                    return BadRequest(ApiResponse<object>.Error(
                        "Formato JSON inválido no campo 'dados'",
                        ErrosRequisicoes.JSON_INVALIDO));
                }

                if (request == null)
                    return BadRequest(ApiResponse<object>.Error(
                        "Dados da ocorrência inválidos",
                        ErrosRequisicoes.DADOS_INVALIDOS));

                var errosValidacao = ValidarRequest(request);
                if (errosValidacao.Count > 0)
                    return BadRequest(ApiResponse<object>.Error(
                        $"Erros de validação: {string.Join(", ", errosValidacao)}",
                        ErrosRequisicoes.VALIDACAO_FALHOU));

                if (comprovante == null)
                    return BadRequest(ApiResponse<object>.Error(
                        "O comprovante de residência é obrigatório",
                        ErrosRequisicoes.ARQUIVOS_AUSENTES));

                if (fotos == null || fotos.Count == 0)
                    return BadRequest(ApiResponse<object>.Error(
                        "É obrigatório enviar ao menos uma foto do local",
                        ErrosRequisicoes.ARQUIVOS_AUSENTES));

                // Mapeia campos tipados → ArquivoUploadDto com TipoArquivo correto
                request.Arquivos = new List<ArquivoUploadDto>
                {
                    new() { TipoArquivo = TipoArquivo.COMPROVANTE_RESIDENCIA, File = comprovante }
                };
                request.Arquivos.AddRange(fotos.Select(f =>
                    new ArquivoUploadDto { TipoArquivo = TipoArquivo.FOTO_CIDADAO, File = f }));

                const long maxFileSize = 10 * 1024 * 1024; // 10 MB
                var arquivoGrande = request.Arquivos.FirstOrDefault(a => a.File.Length > maxFileSize);
                if (arquivoGrande != null)
                    return BadRequest(ApiResponse<object>.Error(
                        $"Arquivo '{arquivoGrande.File.FileName}' excede o tamanho máximo de 10MB",
                        ErrosRequisicoes.ARQUIVO_MUITO_GRANDE));

                var resultado = await _ocorrenciaService.CriarOcorrenciaAsync(request);

                _logger.LogInformation("Ocorrência criada. Protocolo: {Protocolo}", resultado.Protocolo);

                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<OcorrenciaCriadaDto>.Success(resultado, "Ocorrência registrada com sucesso"));
            }
            catch (StorageException ex)
            {
                _logger.LogError(ex, "Falha ao salvar arquivos no disco. IP: {IP}, Tipo: {Tipo}", ipOrigem, ex.TipoErro);
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Error(
                        "Sistema temporariamente indisponível. Tente novamente em alguns minutos.",
                        ErrosRequisicoes.UPLOAD_FAILED));
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, "CriarOcorrencia");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // GET /api/v1/ocorrencias — Listar (dados mascarados LGPD)
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lista ocorrências com dados pessoais mascarados (LGPD).
        /// Suporta filtros por status, grau de risco, emergência, bairro e período.
        /// </summary>
        /// <response code="200">Lista de ocorrências mascaradas</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<OcorrenciaListagemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarOcorrencias(
            [FromQuery] StatusOcorrencia? status,
            [FromQuery] GrauRisco? grauRisco,
            [FromQuery] bool? emergencia,
            [FromQuery] string? bairro,
            [FromQuery] string? protocolo,
            [FromQuery] int? vistoriadorId,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanhoPagina = 50)
        {
            try
            {
                var filtros = new FiltroOcorrenciaDto
                {
                    Status = status,
                    GrauRiscoInicial = grauRisco,
                    Emergencia = emergencia,
                    Bairro = bairro,
                    Protocolo = protocolo,
                    VistoriadorId = vistoriadorId,
                    DataInicio = dataInicio,
                    DataFim = dataFim
                };

                var paginacao = new PaginacaoDto
                {
                    PaginaAtual = pagina,
                    ItensPorPagina = Math.Min(tamanhoPagina, 100)
                };

                var resultado = await _ocorrenciaService.ListarOcorrenciasMascaradasAsync(filtros, paginacao);

                return Ok(ApiResponse<List<OcorrenciaListagemDto>>.Success(
                    resultado, $"{resultado.Count} ocorrência(s) encontrada(s)"));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, "ListarOcorrencias");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // GET /api/v1/ocorrencias/{id} — Detalhe completo (mascarado)
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna o detalhe completo de uma ocorrência com todas as etapas preenchidas.
        /// Dados do solicitante são mascarados por padrão (LGPD).
        /// </summary>
        /// <param name="id">ID da ocorrência</param>
        /// <response code="200">Detalhe completo da ocorrência</response>
        /// <response code="404">Ocorrência não encontrada</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<OcorrenciaDetalheDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterDetalhe([FromRoute] int id)
        {
            try
            {
                var resultado = await _ocorrenciaService.ObterDetalhesAsync(id);
                return Ok(ApiResponse<OcorrenciaDetalheDto>.Success(resultado));
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ObterDetalhe({id})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // PUT /api/v1/ocorrencias/{id} — Atualizar dados da Etapa 1
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Atualiza dados da Etapa 1 (cidadão, local, descrição).
        /// Apenas os campos enviados são alterados (semântica PATCH).
        /// </summary>
        /// <param name="id">ID da ocorrência</param>
        /// <param name="request">Campos a atualizar</param>
        /// <response code="200">Dados atualizados</response>
        /// <response code="404">Ocorrência não encontrada</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<OcorrenciaCriadaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AtualizarOcorrencia(
            [FromRoute] int id,
            [FromBody] AtualizarOcorrenciaRequest request)
        {
            try
            {
                var resultado = await _ocorrenciaService.AtualizarOcorrenciaAsync(id, request, ObterUsuarioIdInterno());
                return Ok(ApiResponse<OcorrenciaCriadaDto>.Success(resultado, "Ocorrência atualizada com sucesso"));
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"AtualizarOcorrencia({id})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // DELETE /api/v1/ocorrencias/{id} — Soft-delete
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Exclui logicamente uma ocorrência (soft-delete).
        /// O registro continua no banco para auditoria; status é alterado para CANCELADA.
        /// </summary>
        /// <param name="id">ID da ocorrência</param>
        /// <param name="motivo">Motivo opcional da exclusão</param>
        /// <response code="204">Excluída com sucesso</response>
        /// <response code="404">Ocorrência não encontrada</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExcluirOcorrencia(
            [FromRoute] int id,
            [FromQuery] string? motivo)
        {
            try
            {
                await _ocorrenciaService.ExcluirAsync(id, ObterUsuarioIdInterno(), motivo);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ExcluirOcorrencia({id})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // POST /api/v1/ocorrencias/{id}/restaurar — Restaurar exclusão
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Restaura uma ocorrência previamente excluída (limpa o soft-delete).
        /// Status retorna para CANCELADA — ajuste manual necessário.
        /// </summary>
        /// <param name="id">ID da ocorrência</param>
        /// <response code="200">Ocorrência restaurada</response>
        /// <response code="404">Ocorrência não encontrada ou não está excluída</response>
        [HttpPost("{id:int}/restaurar")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RestaurarOcorrencia([FromRoute] int id)
        {
            try
            {
                await _ocorrenciaService.RestaurarAsync(id, ObterUsuarioIdInterno());
                return Ok(ApiResponse<object>.Success(null, "Ocorrência restaurada com sucesso"));
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"RestaurarOcorrencia({id})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // POST /api/v1/ocorrencias/{id}/revelar-dados — LGPD
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Revela os dados pessoais não mascarados de uma ocorrência.
        /// LGPD: exige justificativa ≥ 10 caracteres e registra auditoria obrigatória.
        /// </summary>
        /// <param name="id">ID da ocorrência</param>
        /// <param name="request">Usuário solicitante e justificativa</param>
        /// <response code="200">Dados revelados com registro de auditoria</response>
        /// <response code="403">Usuário sem permissão (CIDADAO não pode revelar)</response>
        /// <response code="404">Ocorrência não encontrada</response>
        [HttpPost("{id:int}/revelar-dados")]
        [ProducesResponseType(typeof(ApiResponse<OcorrenciaDadosSensiveisDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevelarDados(
            [FromRoute] int id,
            [FromBody] RevelarDadosRequest request)
        {
            try
            {
                var resultado = await _ocorrenciaService.RevelarDadosSensiveisAsync(id, request, ObterIpCliente());
                return Ok(ApiResponse<OcorrenciaDadosSensiveisDto>.Success(resultado));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<object>.Error(ex.Message, ErrosRequisicoes.ACESSO_NEGADO));
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"RevelarDados({id})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // GET /api/v1/ocorrencias/{id}/arquivos/download — Download de arquivo
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Faz o download de um arquivo vinculado a uma ocorrência.
        /// O caminho relativo é validado contra a lista de arquivos da ocorrência (segurança).
        /// </summary>
        /// <param name="id">ID da ocorrência</param>
        /// <param name="caminho">Caminho relativo do arquivo (ex: /2026-0001/Documentos/FOTO_CIDADAO_uuid.jpg)</param>
        /// <response code="200">Stream do arquivo</response>
        /// <response code="404">Ocorrência ou arquivo não encontrado</response>
        [HttpGet("{id:int}/arquivos/download")]
        [Produces("application/octet-stream", "image/jpeg", "image/png", "application/pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadArquivo(
            [FromRoute] int id,
            [FromQuery] string caminho)
        {
            try
            {
                var ocorrencia = await _ocorrenciaService.ObterDetalhesAsync(id);

                var arquivoExiste = ocorrencia.Arquivos
                    .Any(a => string.Equals(a.CaminhoRelativo, caminho, StringComparison.OrdinalIgnoreCase));

                if (!arquivoExiste)
                    return NaoEncontrado("Arquivo não encontrado para esta ocorrência.");

                var stream = await _storageService.LerArquivoAsync(caminho);
                var contentType = ObterContentType(caminho);
                var nomeArquivo = Path.GetFileName(caminho);

                return File(stream, contentType, nomeArquivo);
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (FileNotFoundException)
            {
                return NaoEncontrado("Arquivo não encontrado no armazenamento.");
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"DownloadArquivo({id}, {caminho})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // GET /api/v1/ocorrencias/{id}/arquivos — Central de Documentos
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lista os arquivos de uma ocorrência para exibição na Central de Documentos.
        /// Suporta filtro por categoria via <paramref name="tipoArquivo"/> para lazy loading
        /// por categoria no frontend (omitir para retornar todos os arquivos de uma vez).
        /// </summary>
        /// <param name="id">ID da ocorrência</param>
        /// <param name="tipoArquivo">
        /// Filtro por categoria (string enum).
        /// Valores aceitos: FOTO_CIDADAO | COMPROVANTE_RESIDENCIA | FICHA_VISTORIA | FOTO_CAMPO | RELATORIO_FINAL
        /// </param>
        /// <response code="200">Lista de arquivos da ocorrência</response>
        /// <response code="404">Ocorrência não encontrada</response>
        [HttpGet("{id:int}/arquivos")]
        [ProducesResponseType(typeof(ApiResponse<List<ArquivoListagemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListarArquivos(
            [FromRoute] int id,
            [FromQuery] string? tipoArquivo)
        {
            try
            {
                var resultado = await _ocorrenciaService.ListarArquivosAsync(id, tipoArquivo);
                return Ok(ApiResponse<List<ArquivoListagemDto>>.Success(
                    resultado,
                    $"{resultado.Count} arquivo(s) encontrado(s)"));
            }
            catch (InvalidOperationException ex)
            {
                return NaoEncontrado(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, $"ListarArquivos({id}, {tipoArquivo})");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        // HELPERS PRIVADOS
        // ══════════════════════════════════════════════════════════════════════════

        private static List<string> ValidarRequest(CriarOcorrenciaRequest request)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Cidadao?.Nome))
                erros.Add("Nome do cidadão é obrigatório");

            if (string.IsNullOrWhiteSpace(request.Cidadao?.Cpf))
                erros.Add("CPF do cidadão é obrigatório");
            else if (request.Cidadao.Cpf.Length != 11 || !request.Cidadao.Cpf.All(char.IsDigit))
                erros.Add("CPF deve conter 11 dígitos numéricos");

            if (string.IsNullOrWhiteSpace(request.Cidadao?.Email))
                erros.Add("Email do cidadão é obrigatório");

            if (string.IsNullOrWhiteSpace(request.Local?.Endereco))
                erros.Add("Endereço é obrigatório");

            if (string.IsNullOrWhiteSpace(request.Local?.Bairro))
                erros.Add("Bairro é obrigatório");

            if (string.IsNullOrWhiteSpace(request.Local?.Cidade))
                erros.Add("Cidade é obrigatória");

            if (string.IsNullOrWhiteSpace(request.Local?.Uf) || request.Local.Uf.Length != 2)
                erros.Add("UF é obrigatória (2 caracteres)");

            if (string.IsNullOrWhiteSpace(request.DescricaoProblema))
                erros.Add("Descrição do problema é obrigatória");

            return erros;
        }

        private static string ObterContentType(string caminho)
        {
            var ext = Path.GetExtension(caminho).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream",
            };
        }
    }
}
