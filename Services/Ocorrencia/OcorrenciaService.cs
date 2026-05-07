using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Usuairos;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Exceptions;
using SIG_Defesa_Civil.API.Helper;
using SIG_Defesa_Civil.API.Services.Storage;

namespace SIG_Defesa_Civil.API.Services.Ocorrencia
{
    public class OcorrenciaService : IOcorrenciaService
    {
        private readonly DefesaCivilContext _context;
        private readonly IStorageService _storageService;
        private readonly ILogger<OcorrenciaService> _logger;

        public OcorrenciaService(
            DefesaCivilContext context,
            IStorageService storageService,
            ILogger<OcorrenciaService> logger)
        {
            _context = context;
            _storageService = storageService;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // ETAPA 1 — CRIAÇÃO
        // ═══════════════════════════════════════════════════════════════════════════

        public async Task<OcorrenciaCriadaDto> CriarOcorrenciaAsync(CriarOcorrenciaRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Iniciando criação de ocorrência para cidadão {Cpf}", request.Cidadao.Cpf);

                // 1. Buscar ou criar o solicitante a partir do CPF.
                //    Cidadãos não possuem conta prévia — o registro é criado/atualizado
                //    automaticamente. O Id obtido aqui é usado como CriadoPorId e
                //    EnviadoPorUserId, pois o próprio cidadão é o autor da solicitação.
                var solicitante = await GetOrCreateSolicitanteAsync(request.Cidadao);

                // 2. Gerar protocolo via sequence PostgreSQL
                var numeroSequence = await ObterProximoNumeroSequenceAsync();
                var protocolo = $"{DateTime.Now.Year}-{numeroSequence:D4}";
                _logger.LogInformation("Protocolo gerado: {Protocolo}", protocolo);

                // 3. Criar registro principal
                var ocorrencia = new Data.Entities.Tabelas.Ocorrencia.Ocorrencia
                {
                    Protocolo = protocolo,
                    SolicitanteId = solicitante.Id,
                    DescricaoProblema = request.DescricaoProblema,
                    Status = StatusOcorrencia.ABERTA,
                    CriadoPorId = solicitante.Id,   // cidadão == autor da solicitação
                    AbertaEm = DateTime.UtcNow,
                    AtualizadoEm = DateTime.UtcNow
                };

                _context.Ocorrencias.Add(ocorrencia);
                await _context.SaveChangesAsync();

                // 4. Criar localização vinculada
                var localizacao = new Localizacao
                {
                    OcorrenciaId = ocorrencia.Id,
                    Endereco = request.Local.Endereco,
                    Bairro = request.Local.Bairro,
                    Numero = request.Local.Numero,
                    Cep = request.Local.Cep,
                    Complemento = request.Local.Complemento,
                    Cidade = request.Local.Cidade,
                    Uf = request.Local.Uf,
                    Coordenada = request.Local.Coordenada,
                    Referencia = request.Local.Referencia,
                    NumeroIptu = request.Local.NumeroIptu
                };

                _context.Localizacoes.Add(localizacao);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ocorrência {Protocolo} salva com ID {Id}", protocolo, ocorrencia.Id);

                // 5. Salvar arquivos no storage
                var arquivosParaUpload = new List<(Stream FileStream, string FileName, TipoArquivo TipoArquivo)>();

                foreach (var arquivo in request.Arquivos)
                {
                    var extensao = Path.GetExtension(arquivo.File.FileName);
                    var nomeUnico = $"{arquivo.TipoArquivo}_{Guid.NewGuid()}{extensao}";
                    var memoryStream = new MemoryStream();
                    await arquivo.File.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    arquivosParaUpload.Add((memoryStream, nomeUnico, arquivo.TipoArquivo));
                }

                List<string> caminhos;
                try
                {
                    caminhos = await _storageService.SalvarArquivosAsync(protocolo, arquivosParaUpload);
                }
                finally
                {
                    foreach (var (stream, _, _) in arquivosParaUpload)
                        await stream.DisposeAsync();
                }

                // 6. Persistir referências dos arquivos
                foreach (var (caminhoRelativo, arquivoOriginal) in caminhos.Zip(request.Arquivos))
                {
                    _context.Arquivos.Add(new Arquivo
                    {
                        OcorrenciaId = ocorrencia.Id,
                        NomeOriginal = arquivoOriginal.File.FileName,
                        TipoArquivo = arquivoOriginal.TipoArquivo.ToString(),
                        CaminhoRelativo = caminhoRelativo,
                        TamanhoBytes = arquivoOriginal.File.Length,
                        EnviadoPorUserId = solicitante.Id,
                        EnviadoEm = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Ocorrência {Protocolo} criada com {Count} arquivo(s)", protocolo, caminhos.Count);

                // 7. Log LGPD fora da transação (não pode bloquear o fluxo)
                await RegistrarLogLgpdAsync(solicitante.Id, ocorrencia.Id, AcaoLgpd.CRIOU);

                return new OcorrenciaCriadaDto
                {
                    Id = ocorrencia.Id,
                    Protocolo = ocorrencia.Protocolo,
                    AbertaEm = ocorrencia.AbertaEm,
                    Status = ocorrencia.Status,
                    ArquivosSalvos = caminhos.Count
                };
            }
            catch (StorageException ex)
            {
                _logger.LogError(ex, "Falha ao salvar arquivos. Executando rollback.");
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar ocorrência. Executando rollback.");
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Erro ao processar a ocorrência. Tente novamente.", ex);
            }
        }

        public async Task<OcorrenciaDetalheDto> ObterDetalhesAsync(int ocorrenciaId)
        {
            var ocorrencia = await _context.Ocorrencias
                .Include(o => o.Solicitante)
                .Include(o => o.CriadoPor)
                .Include(o => o.Localizacao)
                .Include(o => o.AvaliacaoRisco).ThenInclude(a => a!.AbertaPorUsuario)
                .Include(o => o.Agendamentos).ThenInclude(a => a.Vistoriador1)
                .Include(o => o.Agendamentos).ThenInclude(a => a.Vistoriador2)
                .Include(o => o.Agendamentos).ThenInclude(a => a.AgendadoPor)
                .Include(o => o.Agendamentos).ThenInclude(a => a.Tentativas)
                .Include(o => o.Vistorias).ThenInclude(v => v.RegistradoPor)
                .Include(o => o.Notificados).ThenInclude(n => n.RegistradoPor)
                .Include(o => o.EncaminhamentoFinal).ThenInclude(e => e!.RelatorioVistoria)
                .Include(o => o.EncaminhamentoFinal).ThenInclude(e => e!.RegistradoPor)
                .Include(o => o.Arquivos)
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            return MapearDetalhe(ocorrencia);
        }

        public async Task<OcorrenciaCriadaDto> AtualizarOcorrenciaAsync(
            int ocorrenciaId,
            AtualizarOcorrenciaRequest request,
            int usuarioId)
        {
            var ocorrencia = await _context.Ocorrencias
                .Include(o => o.Localizacao)
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            if (request.DescricaoProblema != null)
                ocorrencia.DescricaoProblema = request.DescricaoProblema;

            if (request.Cidadao != null)
            {
                var solicitante = await _context.Usuarios.FindAsync(ocorrencia.SolicitanteId)!;
                solicitante!.Nome = request.Cidadao.Nome ?? solicitante.Nome;
                solicitante.Email = request.Cidadao.Email ?? solicitante.Email;
                solicitante.Telefone = request.Cidadao.Telefone ?? solicitante.Telefone;
                solicitante.Celular = request.Cidadao.Celular ?? solicitante.Celular;
                solicitante.Rg = request.Cidadao.Rg ?? solicitante.Rg;
                solicitante.OrgaoEmissor = request.Cidadao.OrgaoEmissor ?? solicitante.OrgaoEmissor;
            }

            if (request.Local != null && ocorrencia.Localizacao != null)
            {
                var loc = ocorrencia.Localizacao;
                loc.Endereco = request.Local.Endereco ?? loc.Endereco;
                loc.Bairro = request.Local.Bairro ?? loc.Bairro;
                loc.Numero = request.Local.Numero ?? loc.Numero;
                loc.Cep = request.Local.Cep ?? loc.Cep;
                loc.Complemento = request.Local.Complemento ?? loc.Complemento;
                loc.Cidade = request.Local.Cidade ?? loc.Cidade;
                loc.Uf = request.Local.Uf ?? loc.Uf;
                loc.Coordenada = request.Local.Coordenada ?? loc.Coordenada;
                loc.Referencia = request.Local.Referencia ?? loc.Referencia;
                loc.NumeroIptu = request.Local.NumeroIptu ?? loc.NumeroIptu;
            }

            ocorrencia.AtualizadoEm = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new OcorrenciaCriadaDto
            {
                Id = ocorrencia.Id,
                Protocolo = ocorrencia.Protocolo,
                AbertaEm = ocorrencia.AbertaEm,
                Status = ocorrencia.Status,
                ArquivosSalvos = 0
            };
        }

        public async Task ExcluirAsync(int ocorrenciaId, int usuarioId, string? motivo = null)
        {
            var ocorrencia = await _context.Ocorrencias
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada ou já excluída.");

            ocorrencia.DeletedAt = DateTime.UtcNow;
            ocorrencia.ExcluidoPorId = usuarioId;
            ocorrencia.Status = StatusOcorrencia.CANCELADA;
            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Ocorrência {Protocolo} excluída (soft-delete) pelo usuário {UsuarioId}. Motivo: {Motivo}",
                ocorrencia.Protocolo, usuarioId, motivo ?? "Não informado");

            await RegistrarLogLgpdAsync(usuarioId, ocorrenciaId, AcaoLgpd.EXCLUIU);
        }

        public async Task RestaurarAsync(int ocorrenciaId, int usuarioId)
        {
            var ocorrencia = await _context.Ocorrencias
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId && o.DeletedAt != null)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada ou não está excluída.");

            ocorrencia.DeletedAt = null;
            ocorrencia.ExcluidoPorId = null;
            ocorrencia.Status = StatusOcorrencia.CANCELADA; // status neutro — revisar manualmente
            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Ocorrência {Protocolo} restaurada pelo usuário {UsuarioId}", ocorrencia.Protocolo, usuarioId);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // LISTAGEM E LGPD
        // ═══════════════════════════════════════════════════════════════════════════

        public async Task<List<OcorrenciaListagemDto>> ListarOcorrenciasMascaradasAsync(
            FiltroOcorrenciaDto? filtros = null,
            PaginacaoDto? paginacao = null)
        {
            _logger.LogInformation("Listagem de ocorrências com mascaramento LGPD");

            try
            {
                var query = _context.Ocorrencias
                    .Include(o => o.Solicitante)
                    .Include(o => o.Localizacao)
                    .Include(o => o.AvaliacaoRisco)
                    .Include(o => o.Agendamentos).ThenInclude(a => a.Vistoriador1)
                    .Include(o => o.Arquivos)
                    .Where(o => o.DeletedAt == null)
                    .AsQueryable();

                if (filtros != null)
                    query = AplicarFiltros(query, filtros);

                query = query.OrderByDescending(o => o.AbertaEm);

                query = paginacao != null
                    ? query.Skip(paginacao.Skip).Take(paginacao.Take)
                    : query.Take(50);

                var ocorrencias = await query.ToListAsync();

                return ocorrencias.Select(o => new OcorrenciaListagemDto
                {
                    Id = o.Id,
                    Protocolo = o.Protocolo,
                    Status = o.Status,

                    Solicitante = new CidadaoMascaradoDto
                    {
                        Nome = MascaramentoHelper.MascararNome(o.Solicitante.Nome),
                        Cpf = MascaramentoHelper.MascararCpf(o.Solicitante.Cpf ?? string.Empty),
                        Email = MascaramentoHelper.MascararEmail(o.Solicitante.Email),
                        Telefone = MascaramentoHelper.MascararTelefone(o.Solicitante.Telefone ?? string.Empty)
                    },

                    Bairro = o.Localizacao?.Bairro ?? string.Empty,
                    Cidade = o.Localizacao?.Cidade ?? string.Empty,

                    GrauRiscoInicial = o.AvaliacaoRisco?.GrauRiscoInicial,
                    TipificacaoInicial = o.AvaliacaoRisco?.TipificacaoInicial,
                    Emergencia = o.AvaliacaoRisco?.Emergencia,

                    NomeVistoriador1 = o.Agendamentos
                        .OrderByDescending(a => a.Numero)
                        .FirstOrDefault()?.Vistoriador1.Nome,

                    AbertaEm = o.AbertaEm,
                    AtualizadoEm = o.AtualizadoEm,
                    QuantidadeArquivos = o.Arquivos.Count
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar ocorrências mascaradas");
                throw new InvalidOperationException("Erro ao listar ocorrências", ex);
            }
        }

        public async Task<OcorrenciaDadosSensiveisDto> RevelarDadosSensiveisAsync(
            int ocorrenciaId,
            RevelarDadosRequest request,
            string ipOrigem)
        {
            _logger.LogInformation(
                "Revelação de dados sensíveis. Ocorrência: {OcorrenciaId}, Usuário: {UsuarioId}",
                ocorrenciaId, request.UsuarioId);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(request.Justificativa) || request.Justificativa.Length < 10)
                    throw new InvalidOperationException(
                        "Justificativa obrigatória deve ter no mínimo 10 caracteres (compliance LGPD)");

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == request.UsuarioId && u.Ativo)
                    ?? throw new InvalidOperationException("Usuário inválido ou inativo");

                if (usuario.TipoUsuario == TipoUsuario.CIDADAO)
                    throw new UnauthorizedAccessException(
                        "Cidadãos não têm permissão para acessar dados sensíveis de outras ocorrências");

                var ocorrencia = await _context.Ocorrencias
                    .Include(o => o.Solicitante)
                    .Include(o => o.Localizacao)
                    .Include(o => o.AvaliacaoRisco)
                    .Include(o => o.Arquivos)
                    .Where(o => o.DeletedAt == null)
                    .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                    ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada");

                // Log LGPD obrigatório dentro da transação — se falhar, rollback
                var logAcesso = new LogAcessoLgpd
                {
                    UsuarioId = request.UsuarioId,
                    OcorrenciaId = ocorrenciaId,
                    Acao = AcaoLgpd.VISUALIZOU,
                    IpOrigem = ipOrigem,
                    UserAgent = request.Justificativa,
                    RegistradoEm = DateTime.UtcNow
                };

                _context.LogsLgpd.Add(logAcesso);
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "LGPD: Dados sensíveis acessados. Usuário: {Usuario}, Ocorrência: {Protocolo}, IP: {IP}",
                    usuario.Nome, ocorrencia.Protocolo, ipOrigem);

                await transaction.CommitAsync();

                return new OcorrenciaDadosSensiveisDto
                {
                    Id = ocorrencia.Id,
                    Protocolo = ocorrencia.Protocolo,
                    Status = ocorrencia.Status,

                    Solicitante = new CidadaoCompletoDto
                    {
                        Nome = ocorrencia.Solicitante.Nome,
                        Cpf = ocorrencia.Solicitante.Cpf ?? string.Empty,
                        Rg = ocorrencia.Solicitante.Rg,
                        OrgaoEmissor = ocorrencia.Solicitante.OrgaoEmissor,
                        Email = ocorrencia.Solicitante.Email,
                        Telefone = ocorrencia.Solicitante.Telefone,
                        Celular = ocorrencia.Solicitante.Celular
                    },

                    Localizacao = ocorrencia.Localizacao == null ? null : new LocalizacaoDto
                    {
                        Endereco = ocorrencia.Localizacao.Endereco,
                        Bairro = ocorrencia.Localizacao.Bairro,
                        Numero = ocorrencia.Localizacao.Numero,
                        Cep = ocorrencia.Localizacao.Cep,
                        Complemento = ocorrencia.Localizacao.Complemento,
                        Cidade = ocorrencia.Localizacao.Cidade,
                        Uf = ocorrencia.Localizacao.Uf,
                        Coordenada = ocorrencia.Localizacao.Coordenada,
                        Referencia = ocorrencia.Localizacao.Referencia,
                        NumeroIptu = ocorrencia.Localizacao.NumeroIptu
                    },

                    GrauRiscoInicial = ocorrencia.AvaliacaoRisco?.GrauRiscoInicial,
                    TipificacaoInicial = ocorrencia.AvaliacaoRisco?.TipificacaoInicial,

                    UltimoAcesso = new AcessoLgpdDto
                    {
                        UsuarioQueAcessou = usuario.Nome,
                        DataHoraAcesso = logAcesso.RegistradoEm,
                        IpOrigem = ipOrigem
                    },

                    Documentos = ocorrencia.Arquivos.Select(a => new DocumentoVisualizacao
                    {
                        NomeOriginal = a.NomeOriginal,
                        TipoArquivo = Enum.Parse<TipoArquivo>(a.TipoArquivo),
                        CaminhoRelativo = a.CaminhoRelativo,
                        TamanhoBytes = a.TamanhoBytes,
                        EnviadoPorUserId = a.EnviadoPorUserId,
                        EnviadoEm = a.EnviadoEm
                    }).ToList()
                };
            }
            catch (UnauthorizedAccessException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Erro ao revelar dados sensíveis. Ocorrência: {OcorrenciaId}", ocorrenciaId);
                throw new InvalidOperationException("Erro ao acessar dados sensíveis. Tente novamente.", ex);
            }
        }

        public async Task<List<ArquivoListagemDto>> ListarArquivosAsync(
            int ocorrenciaId,
            string? tipoArquivo = null)
        {
            var existe = await _context.Ocorrencias
                .AnyAsync(o => o.Id == ocorrenciaId && o.DeletedAt == null);

            if (!existe)
                throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            var query = _context.Arquivos
                .Include(a => a.Usuario)
                .Where(a => a.OcorrenciaId == ocorrenciaId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tipoArquivo))
                query = query.Where(a => a.TipoArquivo == tipoArquivo);

            var arquivos = await query
                .OrderBy(a => a.TipoArquivo)
                .ThenBy(a => a.EnviadoEm)
                .ToListAsync();

            return arquivos.Select(a => new ArquivoListagemDto
            {
                Id              = a.Id,
                TipoArquivo     = a.TipoArquivo,
                NomeOriginal    = a.NomeOriginal,
                CaminhoRelativo = a.CaminhoRelativo,
                TamanhoBytes    = a.TamanhoBytes,
                EnviadoPor      = a.Usuario?.Nome,
                EnviadoEm       = a.EnviadoEm
            }).ToList();
        }

        public Task<GeracaoLoteResultadoDto> GerarDocumentosEmLoteAsync(GerarDocumentosLoteRequest request)
        {
            throw new NotImplementedException("Será implementado na Fase seguinte");
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // HELPERS PRIVADOS
        // ═══════════════════════════════════════════════════════════════════════════

        private IQueryable<Data.Entities.Tabelas.Ocorrencia.Ocorrencia> AplicarFiltros(
            IQueryable<Data.Entities.Tabelas.Ocorrencia.Ocorrencia> query,
            FiltroOcorrenciaDto filtros)
        {
            if (filtros.Status.HasValue)
                query = query.Where(o => o.Status == filtros.Status.Value);

            if (filtros.GrauRiscoInicial.HasValue)
                query = query.Where(o => o.AvaliacaoRisco != null &&
                                         o.AvaliacaoRisco.GrauRiscoInicial == filtros.GrauRiscoInicial.Value);

            if (filtros.Emergencia.HasValue)
                query = query.Where(o => o.AvaliacaoRisco != null &&
                                         o.AvaliacaoRisco.Emergencia == filtros.Emergencia.Value);

            if (filtros.VistoriadorId.HasValue)
                query = query.Where(o => o.Agendamentos.Any(a =>
                    a.Vistoriador1Id == filtros.VistoriadorId.Value ||
                    a.Vistoriador2Id == filtros.VistoriadorId.Value));

            if (filtros.DataInicio.HasValue)
                query = query.Where(o => o.AbertaEm >= filtros.DataInicio.Value);

            if (filtros.DataFim.HasValue)
                query = query.Where(o => o.AbertaEm <= filtros.DataFim.Value);

            if (!string.IsNullOrWhiteSpace(filtros.Protocolo))
                query = query.Where(o => o.Protocolo.Contains(filtros.Protocolo));

            if (!string.IsNullOrWhiteSpace(filtros.Bairro))
                query = query.Where(o => o.Localizacao != null &&
                                         o.Localizacao.Bairro.Contains(filtros.Bairro));

            return query;
        }

        private OcorrenciaDetalheDto MapearDetalhe(Data.Entities.Tabelas.Ocorrencia.Ocorrencia o)
        {
            return new OcorrenciaDetalheDto
            {
                Id = o.Id,
                Protocolo = o.Protocolo,
                Status = o.Status,
                DescricaoProblema = o.DescricaoProblema,

                Solicitante = new CidadaoMascaradoDto
                {
                    Nome = MascaramentoHelper.MascararNome(o.Solicitante.Nome),
                    Cpf = MascaramentoHelper.MascararCpf(o.Solicitante.Cpf ?? string.Empty),
                    Email = MascaramentoHelper.MascararEmail(o.Solicitante.Email),
                    Telefone = MascaramentoHelper.MascararTelefone(o.Solicitante.Telefone ?? string.Empty)
                },

                Localizacao = o.Localizacao == null ? null : new LocalizacaoDto
                {
                    Endereco = o.Localizacao.Endereco,
                    Bairro = o.Localizacao.Bairro,
                    Numero = o.Localizacao.Numero,
                    Cep = o.Localizacao.Cep,
                    Complemento = o.Localizacao.Complemento,
                    Cidade = o.Localizacao.Cidade,
                    Uf = o.Localizacao.Uf,
                    Coordenada = o.Localizacao.Coordenada,
                    Referencia = o.Localizacao.Referencia,
                    NumeroIptu = o.Localizacao.NumeroIptu
                },

                AvaliacaoRisco = o.AvaliacaoRisco == null ? null : new AvaliacaoRiscoDto
                {
                    Id = o.AvaliacaoRisco.Id,
                    TipificacaoInicial = o.AvaliacaoRisco.TipificacaoInicial,
                    GrauRiscoInicial = o.AvaliacaoRisco.GrauRiscoInicial,
                    NomeAgenteTriage = o.AvaliacaoRisco.AbertaPorUsuario?.Nome,
                    RequisicaoSetorDocumento = o.AvaliacaoRisco.RequisicaoSetorDocumento,
                    Emergencia = o.AvaliacaoRisco.Emergencia,
                    RegistradoEm = o.AvaliacaoRisco.RegistradoEm,
                    AtualizadoEm = o.AvaliacaoRisco.AtualizadoEm
                },

                Agendamentos = o.Agendamentos
                    .OrderBy(a => a.Numero)
                    .Select(a => new AgendamentoVistoriaDto
                    {
                        Id = a.Id,
                        Numero = a.Numero,
                        Status = a.Status.ToString(),
                        Vistoriador1Id = a.Vistoriador1Id,
                        NomeVistoriador1 = a.Vistoriador1.Nome,
                        MatriculaVistoriador1 = a.Vistoriador1.Matricula,
                        Vistoriador2Id = a.Vistoriador2Id,
                        NomeVistoriador2 = a.Vistoriador2?.Nome,
                        MatriculaVistoriador2 = a.Vistoriador2?.Matricula,
                        AgendadoPor = a.AgendadoPor.Nome,
                        AgendadoEm = a.AgendadoEm,
                        Tentativas = a.Tentativas
                            .OrderBy(t => t.NumeroTentativa)
                            .Select(t => new TentativaVistoriaDto
                            {
                                Id = t.Id,
                                NumeroTentativa = t.NumeroTentativa,
                                DataHoraTentativa = t.DataHoraTentativa,
                                Observacao = t.Observacao
                            }).ToList()
                    }).ToList(),

                Vistorias = o.Vistorias
                    .OrderBy(v => v.Numero)
                    .Select(v => new VistoriaDto
                    {
                        Id = v.Id,
                        Numero = v.Numero,
                        AgendamentoId = v.AgendamentoId,
                        DataVistoria = v.DataVistoria,
                        HorarioInicio = v.HorarioInicio,
                        HorarioTermino = v.HorarioTermino,
                        DescricaoDoLocal = v.DescricaoDoLocal,
                        CaracterizacaoDoLocal = v.CaracterizacaoDoLocal,
                        Edificacao = v.Edificacao,
                        Estrutura = v.Estrutura,
                        NumeroMoradias = v.NumeroMoradias,
                        NumeroComodos = v.NumeroComodos,
                        NumeroPavimentos = v.NumeroPavimentos,
                        NumeroMoradiasNoLote = v.NumeroMoradiasNoLote,
                        PossuiUnidadeFamiliar = v.PossuiUnidadeFamiliar,
                        NumeroAdultos = v.NumeroAdultos,
                        NumeroCriancas = v.NumeroCriancas,
                        NumeroIdosos = v.NumeroIdosos,
                        NumeroDeficientes = v.NumeroDeficientes,
                        TotalMoradores = v.TotalMoradores,
                        TipoRisco = v.TipoRisco,
                        GrauRiscoEncontrado = v.GrauRiscoEncontrado,
                        TipificacaoOcorrencia = v.TipificacaoOcorrencia,
                        RegimeOcupacao = v.RegimeOcupacao,
                        Motivacao = v.Motivacao,
                        AreasAfetadas = v.AreasAfetadas,
                        Interdicao = v.Interdicao,
                        Remocao = v.Remocao,
                        Orientacoes = v.Orientacoes,
                        EncaminhamentosDeCampo = v.EncaminhamentosDeCampo,
                        RegistradoPor = v.RegistradoPor.Nome,
                        RegistradoEm = v.RegistradoEm
                    }).ToList(),

                Notificados = o.Notificados.Select(n => new NotificadoDto
                {
                    Id = n.Id,
                    Nome = n.Nome,
                    RgCpf = n.RgCpf,
                    DataNotificacao = n.DataNotificacao,
                    RegistradoPor = n.RegistradoPor.Nome,
                    RegistradoEm = n.RegistradoEm
                }).ToList(),

                EncaminhamentoFinal = o.EncaminhamentoFinal == null ? null : new EncaminhamentoFinalDto
                {
                    Id = o.EncaminhamentoFinal.Id,
                    Encaminhamentos = o.EncaminhamentoFinal.Encaminhamentos,
                    RetornoEncaminhamentos = o.EncaminhamentoFinal.RetornoEncaminhamentos,
                    EntregaRelatorio = o.EncaminhamentoFinal.EntregaRelatorio,
                    RelatorioVistoria = o.EncaminhamentoFinal.RelatorioVistoria == null ? null : new DocumentoVisualizacao
                    {
                        NomeOriginal = o.EncaminhamentoFinal.RelatorioVistoria.NomeOriginal,
                        TipoArquivo = Enum.Parse<TipoArquivo>(o.EncaminhamentoFinal.RelatorioVistoria.TipoArquivo),
                        CaminhoRelativo = o.EncaminhamentoFinal.RelatorioVistoria.CaminhoRelativo,
                        TamanhoBytes = o.EncaminhamentoFinal.RelatorioVistoria.TamanhoBytes,
                        EnviadoPorUserId = o.EncaminhamentoFinal.RelatorioVistoria.EnviadoPorUserId,
                        EnviadoEm = o.EncaminhamentoFinal.RelatorioVistoria.EnviadoEm
                    },
                    RegistradoPor = o.EncaminhamentoFinal.RegistradoPor.Nome,
                    RegistradoEm = o.EncaminhamentoFinal.RegistradoEm,
                    AtualizadoEm = o.EncaminhamentoFinal.AtualizadoEm
                },

                Arquivos = o.Arquivos.Select(a => new DocumentoVisualizacao
                {
                    NomeOriginal = a.NomeOriginal,
                    TipoArquivo = Enum.Parse<TipoArquivo>(a.TipoArquivo),
                    CaminhoRelativo = a.CaminhoRelativo,
                    TamanhoBytes = a.TamanhoBytes,
                    EnviadoPorUserId = a.EnviadoPorUserId,
                    EnviadoEm = a.EnviadoEm
                }).ToList(),

                CriadoPor = o.CriadoPor.Nome,
                AbertaEm = o.AbertaEm,
                AtualizadoEm = o.AtualizadoEm
            };
        }

        private async Task<Usuario> GetOrCreateSolicitanteAsync(
            Data.DTO.Requests.Usuarios.CidadaoDto dto)
        {
            var existente = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Cpf == dto.Cpf);

            if (existente != null)
            {
                existente.Nome = dto.Nome;
                existente.Email = dto.Email;
                existente.Telefone = dto.Telefone;
                existente.Celular = dto.Celular;
                existente.Rg = dto.Rg;
                existente.OrgaoEmissor = dto.OrgaoEmissor;
                await _context.SaveChangesAsync();
                return existente;
            }

            var novo = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Cpf = dto.Cpf,
                Rg = dto.Rg,
                OrgaoEmissor = dto.OrgaoEmissor,
                Telefone = dto.Telefone,
                Celular = dto.Celular,
                TipoUsuario = TipoUsuario.CIDADAO,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            };

            _context.Usuarios.Add(novo);
            await _context.SaveChangesAsync();
            return novo;
        }

        private async Task<int> ObterProximoNumeroSequenceAsync()
        {
            try
            {
                var resultado = await _context.Database
                    .SqlQuery<int>($"SELECT nextval('seq_protocolo_ano')")
                    .ToListAsync();

                var proximo = resultado.FirstOrDefault();
                if (proximo == 0)
                    throw new InvalidOperationException("Falha ao obter número da sequence");

                return proximo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter próximo número da sequence");
                throw new InvalidOperationException("Erro ao gerar protocolo. Tente novamente.", ex);
            }
        }

        private async Task RegistrarLogLgpdAsync(int usuarioId, int ocorrenciaId, AcaoLgpd acao)
        {
            try
            {
                _context.LogsLgpd.Add(new LogAcessoLgpd
                {
                    UsuarioId = usuarioId,
                    OcorrenciaId = ocorrenciaId,
                    Acao = acao,
                    IpOrigem = "Sistema",
                    RegistradoEm = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao registrar log LGPD para usuário {UsuarioId}", usuarioId);
            }
        }
    }
}
