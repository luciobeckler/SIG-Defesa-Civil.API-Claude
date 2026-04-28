using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Usuarios;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.SharePoint;
using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Exceptions;
using SIG_Defesa_Civil.API.Services.SharePoint.SIG_Defesa_Civil.API;

namespace SIG_Defesa_Civil.API.Services.Ocorrencia
{
    public class OcorrenciaService : IOcorrenciaService
    {
        private readonly DefesaCivilContext _context;
        private readonly ISharePointService _sharePointService;
        private readonly ILogger<OcorrenciaService> _logger;

        public OcorrenciaService(
            DefesaCivilContext context,
            ISharePointService sharePointService,
            ILogger<OcorrenciaService> logger)
        {
            _context = context;
            _sharePointService = sharePointService;
            _logger = logger;
        }

        public async Task<OcorrenciaCriadaDto> CriarOcorrenciaAsync(CriarOcorrenciaRequest request)
        {
            // Iniciar transação
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Iniciando criação de ocorrência para cidadão {Cpf}", request.Cidadao.Cpf);

                // 1. Buscar ou criar cidadão
                var cidadao = await GetOrCreateCidadaoAsync(request.Cidadao);

                // 2. Gerar protocolo usando a sequence do PostgreSQL
                var numeroSequence = await ObterProximoNumeroSequenceAsync();
                var anoAtual = DateTime.Now.Year;
                var protocolo = $"{anoAtual}-{numeroSequence:D4}"; // Formato: 2025-0042

                _logger.LogInformation("Protocolo gerado: {Protocolo}", protocolo);

                // 3. Criar registro da ocorrência
                var ocorrencia = new Data.Models.Tabelas.Ocorrencia
                {
                    Protocolo = protocolo,
                    CidadaoId = cidadao.Id,
                    EnderecoCompleto = request.Local.EnderecoCompleto,
                    Latitude = request.Local.Latitude,
                    Longitude = request.Local.Longitude,
                    Status = Enums.StatusOcorrencia.ABERTA,
                    AbertaEm = DateTime.UtcNow
                };

                _context.Ocorrencias.Add(ocorrencia);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Ocorrência {Protocolo} salva no banco com ID {Id}",
                    protocolo,
                    ocorrencia.Id);

                // 4. Preparar arquivos para upload no SharePoint
                var arquivosParaUpload = new List<(Stream FileStream, string FileName, TipoArquivo TipoArquivo)>();

                foreach (var arquivo in request.Arquivos)
                {
                    // Gerar nome único para evitar conflitos
                    var extensao = Path.GetExtension(arquivo.File.FileName);
                    var nomeUnico = $"{arquivo.TipoArquivo}_{Guid.NewGuid()}{extensao}";

                    // IFormFile já é um stream, mas precisamos de uma cópia para não fechar o stream original
                    var memoryStream = new MemoryStream();
                    await arquivo.File.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    arquivosParaUpload.Add((memoryStream, nomeUnico, arquivo.TipoArquivo));
                }

                // 5. Upload no SharePoint (SEM retry - falha = exceção)
                List<SharePointUploadResult> sharePointResults;

                try
                {
                    sharePointResults = await _sharePointService.UploadArquivosAsync(
                        protocolo,
                        arquivosParaUpload);
                }
                finally
                {
                    // Limpar streams
                    foreach (var (stream, _, _) in arquivosParaUpload)
                    {
                        await stream.DisposeAsync();
                    }
                }

                // 6. Salvar referências dos arquivos no banco
                foreach (var (resultado, arquivoOriginal) in sharePointResults.Zip(request.Arquivos))
                {
                    var arquivoEntity = new Arquivo
                    {
                        OcorrenciaId = ocorrencia.Id,
                        NomeOriginal = arquivoOriginal.File.FileName,
                        TipoArquivo = arquivoOriginal.TipoArquivo,
                        SharepointId = resultado.ItemId,
                        SharepointUrl = resultado.WebUrl,
                        EnviadoPor = cidadao.Id,
                        EnviadoEm = DateTime.UtcNow
                    };

                    _context.Arquivos.Add(arquivoEntity);
                }

                await _context.SaveChangesAsync();

                // 7. Commit da transação
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Transação commitada com sucesso. Ocorrência {Protocolo} criada com {Count} arquivo(s)",
                    protocolo,
                    sharePointResults.Count);

                // 8. Registrar log LGPD (fora da transação)
                await RegistrarLogLgpdAsync(cidadao.Id, ocorrencia.Id, AcaoLgpd.CRIOU);

                // 9. Retornar DTO de resposta
                return new OcorrenciaCriadaDto
                {
                    Id = ocorrencia.Id,
                    Protocolo = ocorrencia.Protocolo,
                    AbertaEm = ocorrencia.AbertaEm,
                    Status = ocorrencia.Status,
                    ArquivosSalvos = sharePointResults.Count
                };
            }
            catch (SharePointUploadException ex)
            {
                // Rollback em caso de falha no SharePoint
                _logger.LogError(ex,
                    "Falha no upload SharePoint. Executando rollback da transação.");

                await transaction.RollbackAsync();

                // Re-lançar para o controller tratar e retornar HTTP 503
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar ocorrência. Executando rollback.");

                await transaction.RollbackAsync();

                throw new InvalidOperationException(
                    "Erro ao processar a ocorrência. Tente novamente.",
                    ex);
            }
        }

        public Task<GeracaoLoteResultadoDto> GerarDocumentosEmLoteAsync(GerarDocumentosLoteRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<List<OcorrenciaListagemDto>> ListarOcorrenciasMascaradasAsync(FiltroOcorrenciaDto? filtros = null, PaginacaoDto? paginacao = null)
        {
            throw new NotImplementedException();
        }

        public Task<OcorrenciaDadosSensiveisDto> RevelarDadosSensiveisAsync(int ocorrenciaId, RevelarDadosRequest request, string ipOrigem)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Busca ou cria um cidadão no banco de dados
        /// </summary>
        private async Task<Usuario> GetOrCreateCidadaoAsync(CidadaoDto dto)
        {
            // Tentar buscar por CPF
            var cidadaoExistente = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Cpf == dto.Cpf);

            if (cidadaoExistente != null)
            {
                // Atualizar dados se necessário
                cidadaoExistente.Nome = dto.Nome;
                cidadaoExistente.Email = dto.Email;
                cidadaoExistente.Telefone = dto.Telefone;
                cidadaoExistente.Rg = dto.Rg;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Cidadão existente atualizado: CPF {Cpf}", dto.Cpf);
                return cidadaoExistente;
            }

            // Criar novo cidadão
            var novoCidadao = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Cpf = dto.Cpf,
                Rg = dto.Rg,
                Telefone = dto.Telefone,
                TipoUsuario = TipoUsuario.CIDADAO,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            };

            _context.Usuarios.Add(novoCidadao);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Novo cidadão criado: CPF {Cpf}, ID {Id}", dto.Cpf, novoCidadao.Id);
            return novoCidadao;
        }

        /// <summary>
        /// Obtém o próximo número da sequence do PostgreSQL (chamada nativa via EF Core)
        /// </summary>
        private async Task<int> ObterProximoNumeroSequenceAsync()
        {
            try
            {
                // Executar SELECT nextval('seq_protocolo_ano') diretamente no PostgreSQL
                var resultado = await _context.Database
                    .SqlQuery<int>($"SELECT nextval('seq_protocolo_ano')")
                    .ToListAsync();

                var proximoNumero = resultado.FirstOrDefault();

                if (proximoNumero == 0)
                {
                    throw new InvalidOperationException("Falha ao obter número da sequence");
                }

                return proximoNumero;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter próximo número da sequence");
                throw new InvalidOperationException(
                    "Erro ao gerar protocolo. Tente novamente.",
                    ex);
            }
        }

        /// <summary>
        /// Registra ação no log de auditoria LGPD (executado fora da transação)
        /// </summary>
        private async Task RegistrarLogLgpdAsync(int usuarioId, int ocorrenciaId, AcaoLgpd acao)
        {
            try
            {
                var log = new LogAcessoLgpd
                {
                    UsuarioId = usuarioId,
                    OcorrenciaId = ocorrenciaId,
                    Acao = acao,
                    IpOrigem = "Sistema", // Será preenchido pelo controller com o IP real
                    RegistradoEm = DateTime.UtcNow
                };

                _context.LogsLgpd.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log de auditoria não deve quebrar o fluxo principal
                _logger.LogWarning(ex,
                    "Falha ao registrar log LGPD para usuário {UsuarioId}",
                    usuarioId);
            }
        }
    }
}
