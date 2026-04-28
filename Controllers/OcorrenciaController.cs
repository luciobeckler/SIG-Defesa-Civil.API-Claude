using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Exceptions;
using SIG_Defesa_Civil.API.Services;
using SIG_Defesa_Civil.API.Services.Ocorrencia;
using System.Text.Json;

namespace SIG_Defesa_Civil.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class OcorrenciaController : ControllerBase
    {
        private readonly IOcorrenciaService _ocorrenciaService;
        private readonly ILogger<OcorrenciaController> _logger;

        public OcorrenciaController(
            IOcorrenciaService ocorrenciaService,
            ILogger<OcorrenciaController> logger)
        {
            _ocorrenciaService = ocorrenciaService;
            _logger = logger;
        }

        /// <summary>
        /// Cria uma nova ocorrência de Defesa Civil
        /// </summary>
        /// <param name="dados">JSON com dados estruturados (cidadão, local, descrição)</param>
        /// <param name="arquivos">Lista de arquivos (fotos, comprovantes) via multipart/form-data</param>
        /// <returns>Protocolo gerado e dados da ocorrência criada</returns>
        /// <response code="201">Ocorrência criada com sucesso</response>
        /// <response code="400">Dados inválidos ou ausentes</response>
        /// <response code="503">Sistema temporariamente indisponível (falha no SharePoint)</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<OcorrenciaCriadaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CriarOcorrencia(
            [FromForm] string dados,
            [FromForm] List<IFormFile>? arquivos)
        {
            var ipOrigem = ObterIpCliente();

            _logger.LogInformation(
                "Recebida requisição de criação de ocorrência. IP: {IP}, Arquivos: {Count}",
                ipOrigem,
                arquivos?.Count ?? 0);

            try
            {
                // 1. Validar presença de dados
                if (string.IsNullOrWhiteSpace(dados))
                {
                    return BadRequest(ApiResponse<object>.Error(
                        "O campo 'dados' é obrigatório",
                        ErrosRequisicoes.DADOS_AUSENTES));
                }

                // 2. Deserializar JSON dos dados estruturados
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
                {
                    return BadRequest(ApiResponse<object>.Error(
                        "Dados da ocorrência inválidos",
                        ErrosRequisicoes.DADOS_INVALIDOS));
                }

                // 3. Validar campos obrigatórios
                var errosValidacao = ValidarRequest(request);
                if (errosValidacao.Count > 0)
                {
                    return BadRequest(ApiResponse<object>.Error(
                        $"Erros de validação: {string.Join(", ", errosValidacao)}",
                        ErrosRequisicoes.VALIDACAO_FALHOU));
                }

                // 4. Processar arquivos
                if (arquivos == null || arquivos.Count == 0)
                {
                    return BadRequest(ApiResponse<object>.Error(
                        "É obrigatório enviar ao menos uma foto ou documento",
                        ErrosRequisicoes.ARQUIVOS_AUSENTES));
                }

                // Mapear IFormFile para ArquivoUploadDto
                request.Arquivos = arquivos.Select((arquivo, index) =>
                {
                    // Identificar tipo do arquivo pela ordem ou nome
                    // Para MVP, podemos assumir ordem fixa ou usar convenção de nomes
                    var tipoArquivo = DeterminarTipoArquivo(arquivo.FileName, index);

                    return new ArquivoUploadDto
                    {
                        TipoArquivo = tipoArquivo,
                        File = arquivo
                    };
                }).ToList();

                // Validar tamanho dos arquivos (limite de 10MB por arquivo)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                var arquivoGrande = request.Arquivos.FirstOrDefault(a => a.File.Length > maxFileSize);
                if (arquivoGrande != null)
                {
                    return BadRequest(ApiResponse<object>.Error(
                        $"Arquivo '{arquivoGrande.File.FileName}' excede o tamanho máximo de 10MB",
                        ErrosRequisicoes.ARQUIVO_MUITO_GRANDE));
                }

                // 5. Chamar serviço para criar ocorrência (com transação)
                var resultado = await _ocorrenciaService.CriarOcorrenciaAsync(request);

                _logger.LogInformation(
                    "Ocorrência criada com sucesso. Protocolo: {Protocolo}",
                    resultado.Protocolo);

                // 6. Retornar HTTP 201 Created
                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<OcorrenciaCriadaDto>.Success(
                        resultado,
                        "Ocorrência registrada com sucesso"));
            }
            catch (SharePointUploadException ex)
            {
                // Falha no SharePoint = HTTP 503 (Service Unavailable)
                _logger.LogError(ex,
                    "Falha no upload SharePoint. IP: {IP}",
                    ipOrigem);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Error(
                        "Sistema temporariamente indisponível. Não foi possível processar sua solicitação. Tente novamente em alguns minutos.",
                        ErrosRequisicoes.UPLOAD_FAILED));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erro de operação ao criar ocorrência. IP: {IP}", ipOrigem);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<object>.Error(
                        "Erro ao processar a ocorrência. Tente novamente.",
                        ErrosRequisicoes.ERRO_PROCESSAMENTO));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar ocorrência. IP: {IP}", ipOrigem);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<object>.Error(
                        "Erro interno do servidor. Tente novamente mais tarde.",
                        ErrosRequisicoes.ERRO_INTERNO));
            }
        }

        /// <summary>
        /// Valida os campos obrigatórios do request
        /// </summary>
        private List<string> ValidarRequest(CriarOcorrenciaRequest request)
        {
            var erros = new List<string>();

            // Validar cidadão
            if (string.IsNullOrWhiteSpace(request.Cidadao?.Nome))
                erros.Add("Nome do cidadão é obrigatório");

            if (string.IsNullOrWhiteSpace(request.Cidadao?.Cpf))
                erros.Add("CPF do cidadão é obrigatório");
            else if (request.Cidadao.Cpf.Length != 11 || !request.Cidadao.Cpf.All(char.IsDigit))
                erros.Add("CPF deve conter 11 dígitos numéricos");

            if (string.IsNullOrWhiteSpace(request.Cidadao?.Email))
                erros.Add("Email do cidadão é obrigatório");

            if (string.IsNullOrWhiteSpace(request.Cidadao?.Telefone))
                erros.Add("Telefone do cidadão é obrigatório");

            // Validar local
            if (string.IsNullOrWhiteSpace(request.Local?.EnderecoCompleto))
                erros.Add("Endereço completo é obrigatório");

            // Validar descrição
            if (string.IsNullOrWhiteSpace(request.DescricaoProblema))
                erros.Add("Descrição do problema é obrigatória");

            return erros;
        }

        /// <summary>
        /// Determina o tipo do arquivo baseado no nome ou posição
        /// </summary>
        private TipoArquivo DeterminarTipoArquivo(string nomeArquivo, int indice)
        {
            var nomeLower = nomeArquivo.ToLowerInvariant();

            // Tentar identificar por palavras-chave no nome
            if (nomeLower.Contains("comprovante") || nomeLower.Contains("residencia"))
                return TipoArquivo.COMPROVANTE_RESIDENCIA;

            // Por padrão, considerar como foto do cidadão
            // Em produção, o front-end deveria enviar metadados explícitos
            return TipoArquivo.FOTO_CIDADAO;
        }

        /// <summary>
        /// Obtém o endereço IP do cliente (suporta proxy reverso)
        /// </summary>
        private string ObterIpCliente()
        {
            // Verificar headers de proxy reverso (nginx, load balancer)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For pode conter múltiplos IPs separados por vírgula
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback para IP direto da conexão
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";
        }
    }
}
