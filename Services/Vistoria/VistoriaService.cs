using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Services.Storage;

namespace SIG_Defesa_Civil.API.Services.Vistoria
{
    public class VistoriaService : IVistoriaService
    {
        private readonly DefesaCivilContext _context;
        private readonly IStorageService _storageService;
        private readonly ILogger<VistoriaService> _logger;

        private const int MaxTentativas = 3;

        public VistoriaService(
            DefesaCivilContext context,
            IStorageService storageService,
            ILogger<VistoriaService> logger)
        {
            _context = context;
            _storageService = storageService;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // ETAPA 3 — AGENDAMENTO
        // ═══════════════════════════════════════════════════════════════════════════

        public async Task<AgendamentoVistoriaDto> AgendarAsync(
            int ocorrenciaId,
            RegistrarAgendamentoVistoriaRequest request,
            int usuarioId)
        {
            var ocorrencia = await _context.Ocorrencias
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            // Permite agendamento no primeiro ciclo (EM_AVALIACAO) ou no re-agendamento (VISTORIA_SOLICITADA)
            if (ocorrencia.Status != StatusOcorrencia.EM_AVALIACAO &&
                ocorrencia.Status != StatusOcorrencia.VISTORIA_SOLICITADA)
                throw new InvalidOperationException(
                    $"O agendamento só pode ser criado quando a ocorrência está EM_AVALIACAO ou VISTORIA_SOLICITADA. " +
                    $"Status atual: {ocorrencia.Status}.");

            if (request.Vistoriador2Id.HasValue && request.Vistoriador2Id == request.Vistoriador1Id)
                throw new InvalidOperationException("O vistoriador 1 e o vistoriador 2 não podem ser o mesmo usuário.");

            await ValidarVistoriadorAsync(request.Vistoriador1Id, 1);
            if (request.Vistoriador2Id.HasValue)
                await ValidarVistoriadorAsync(request.Vistoriador2Id.Value, 2);

            // Auto-incrementa o número do agendamento dentro da ocorrência
            var proximoNumero = await _context.AgendamentosVistoria
                .Where(a => a.OcorrenciaId == ocorrenciaId)
                .MaxAsync(a => (int?)a.Numero) ?? 0;
            proximoNumero += 1;

            var agendamento = new AgendamentoVistoria
            {
                OcorrenciaId = ocorrenciaId,
                Numero = proximoNumero,
                Status = StatusAgendamento.ATIVO,
                Vistoriador1Id = request.Vistoriador1Id,
                Vistoriador2Id = request.Vistoriador2Id,
                AgendadoPorId = usuarioId,
                AgendadoEm = DateTime.UtcNow
            };

            _context.AgendamentosVistoria.Add(agendamento);
            await _context.SaveChangesAsync(); // necessário para obter o Id do agendamento

            // Primeira tentativa criada automaticamente junto com o agendamento
            var primeiraTentativa = new TentativaVistoria
            {
                AgendamentoId = agendamento.Id,
                NumeroTentativa = 1,
                DataHoraTentativa = request.DataHoraPrimeiraTentativa,
                Observacao = request.Observacao,
                RegistradoEm = DateTime.UtcNow
            };

            _context.TentativasVistoria.Add(primeiraTentativa);

            // Avança o status apenas se ainda estava em EM_AVALIACAO
            if (ocorrencia.Status == StatusOcorrencia.EM_AVALIACAO)
            {
                ocorrencia.Status = StatusOcorrencia.VISTORIA_SOLICITADA;
                ocorrencia.AtualizadoEm = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Agendamento #{Numero} criado. Ocorrência {Protocolo} → VISTORIA_SOLICITADA. " +
                "Vistoriadores: {V1} / {V2}",
                proximoNumero, ocorrencia.Protocolo,
                request.Vistoriador1Id, request.Vistoriador2Id?.ToString() ?? "-");

            return await ObterAgendamentoDtoAsync(agendamento.Id);
        }

        public async Task<List<AgendamentoVistoriaDto>> ListarAgendamentosAsync(int ocorrenciaId)
        {
            var agendamentos = await _context.AgendamentosVistoria
                .Include(a => a.Vistoriador1)
                .Include(a => a.Vistoriador2)
                .Include(a => a.AgendadoPor)
                .Include(a => a.Tentativas)
                .Where(a => a.OcorrenciaId == ocorrenciaId)
                .OrderBy(a => a.Numero)
                .ToListAsync();

            return agendamentos.Select(MapearAgendamentoDto).ToList();
        }

        public async Task<AgendamentoVistoriaDto?> ObterAgendamentoPorIdAsync(int agendamentoId)
        {
            var agendamento = await _context.AgendamentosVistoria
                .Include(a => a.Vistoriador1)
                .Include(a => a.Vistoriador2)
                .Include(a => a.AgendadoPor)
                .Include(a => a.Tentativas)
                .FirstOrDefaultAsync(a => a.Id == agendamentoId);

            return agendamento == null ? null : MapearAgendamentoDto(agendamento);
        }

        public async Task<AgendamentoVistoriaDto> AdicionarTentativaAsync(
            int agendamentoId,
            AdicionarTentativaRequest request,
            int usuarioId)
        {
            var agendamento = await _context.AgendamentosVistoria
                .Include(a => a.Vistoriador1)
                .Include(a => a.Vistoriador2)
                .Include(a => a.AgendadoPor)
                .Include(a => a.Tentativas)
                .FirstOrDefaultAsync(a => a.Id == agendamentoId)
                ?? throw new InvalidOperationException($"Agendamento {agendamentoId} não encontrado.");

            if (agendamento.Status != StatusAgendamento.ATIVO)
                throw new InvalidOperationException(
                    $"Tentativas só podem ser adicionadas a agendamentos ATIVOS. " +
                    $"Status atual: {agendamento.Status}.");

            if (agendamento.Tentativas.Count >= MaxTentativas)
                throw new InvalidOperationException(
                    $"Limite de {MaxTentativas} tentativas atingido para este agendamento.");

            var novaTentativa = new TentativaVistoria
            {
                AgendamentoId = agendamentoId,
                NumeroTentativa = agendamento.Tentativas.Count + 1,
                DataHoraTentativa = request.DataHoraTentativa,
                Observacao = request.Observacao,
                RegistradoEm = DateTime.UtcNow
            };

            _context.TentativasVistoria.Add(novaTentativa);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Tentativa {Num} adicionada ao agendamento {AgendamentoId}",
                novaTentativa.NumeroTentativa, agendamentoId);

            agendamento.Tentativas.Add(novaTentativa);
            return MapearAgendamentoDto(agendamento);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // ETAPA 4 — VISTORIA PRESENCIAL
        // ═══════════════════════════════════════════════════════════════════════════

        public async Task<VistoriaDto> RegistrarVistoriaAsync(
            int ocorrenciaId,
            RegistrarVistoriaRequest request,
            int usuarioId)
        {
            var ocorrencia = await _context.Ocorrencias
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            if (ocorrencia.Status != StatusOcorrencia.VISTORIA_SOLICITADA)
                throw new InvalidOperationException(
                    $"A vistoria só pode ser registrada quando a ocorrência está VISTORIA_SOLICITADA. " +
                    $"Status atual: {ocorrencia.Status}.");

            if (request.HorarioTermino <= request.HorarioInicio)
                throw new InvalidOperationException("O horário de término deve ser posterior ao horário de início.");

            // Valida o agendamento vinculado (se informado)
            if (request.AgendamentoId.HasValue)
            {
                var agendamentoVinculado = await _context.AgendamentosVistoria
                    .FirstOrDefaultAsync(a => a.Id == request.AgendamentoId.Value
                                           && a.OcorrenciaId == ocorrenciaId);
                if (agendamentoVinculado == null)
                    throw new InvalidOperationException(
                        $"Agendamento {request.AgendamentoId} não encontrado para esta ocorrência.");
            }

            var totalMoradores = request.TotalMoradores
                ?? (request.NumeroAdultos + request.NumeroCriancas + request.NumeroIdosos + request.NumeroDeficientes);

            // Auto-incrementa o número da vistoria dentro da ocorrência
            var proximoNumero = await _context.Vistorias
                .Where(v => v.OcorrenciaId == ocorrenciaId)
                .MaxAsync(v => (int?)v.Numero) ?? 0;
            proximoNumero += 1;

            var vistoria = new Data.Entities.Tabelas.Ocorrencia.Vistoria
            {
                OcorrenciaId = ocorrenciaId,
                Numero = proximoNumero,
                AgendamentoId = request.AgendamentoId,
                DataVistoria = request.DataVistoria,
                HorarioInicio = request.HorarioInicio,
                HorarioTermino = request.HorarioTermino,
                DescricaoDoLocal = request.DescricaoDoLocal,
                CaracterizacaoDoLocal = request.CaracterizacaoDoLocal,
                Edificacao = request.Edificacao,
                Estrutura = request.Estrutura,
                NumeroMoradias = request.NumeroMoradias,
                NumeroComodos = request.NumeroComodos,
                NumeroPavimentos = request.NumeroPavimentos,
                NumeroMoradiasNoLote = request.NumeroMoradiasNoLote,
                PossuiUnidadeFamiliar = request.PossuiUnidadeFamiliar,
                NumeroAdultos = request.NumeroAdultos,
                NumeroCriancas = request.NumeroCriancas,
                NumeroIdosos = request.NumeroIdosos,
                NumeroDeficientes = request.NumeroDeficientes,
                TotalMoradores = totalMoradores,
                TipoRisco = request.TipoRisco,
                GrauRiscoEncontrado = request.GrauRiscoEncontrado,
                TipificacaoOcorrencia = request.TipificacaoOcorrencia,
                RegimeOcupacao = request.RegimeOcupacao,
                Motivacao = request.Motivacao,
                AreasAfetadas = request.AreasAfetadas,
                Interdicao = request.Interdicao,
                Remocao = request.Remocao,
                Orientacoes = request.Orientacoes,
                EncaminhamentosDeCampo = request.EncaminhamentosDeCampo,
                RegistradoPorId = usuarioId,
                RegistradoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };

            _context.Vistorias.Add(vistoria);

            // Marca o agendamento vinculado como CONCLUIDO
            if (request.AgendamentoId.HasValue)
            {
                var agendamento = await _context.AgendamentosVistoria
                    .FirstOrDefaultAsync(a => a.Id == request.AgendamentoId.Value);
                if (agendamento != null)
                    agendamento.Status = StatusAgendamento.CONCLUIDO;
            }

            ocorrencia.Status = StatusOcorrencia.VISTORIA_REALIZADA;
            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Vistoria #{Numero} registrada. Ocorrência {Protocolo} → VISTORIA_REALIZADA",
                proximoNumero, ocorrencia.Protocolo);

            await _context.Entry(vistoria).Reference(v => v.RegistradoPor).LoadAsync();
            return MapearVistoriaDto(vistoria);
        }

        public async Task<List<VistoriaDto>> ListarVistoriasAsync(int ocorrenciaId)
        {
            var vistorias = await _context.Vistorias
                .Include(v => v.RegistradoPor)
                .Where(v => v.OcorrenciaId == ocorrenciaId)
                .OrderBy(v => v.Numero)
                .ToListAsync();

            return vistorias.Select(MapearVistoriaDto).ToList();
        }

        public async Task<VistoriaDto?> ObterVistoriaPorIdAsync(int vistoriaId)
        {
            var vistoria = await _context.Vistorias
                .Include(v => v.RegistradoPor)
                .FirstOrDefaultAsync(v => v.Id == vistoriaId);

            return vistoria == null ? null : MapearVistoriaDto(vistoria);
        }

        public async Task<VistoriaDto> AtualizarVistoriaPorIdAsync(
            int vistoriaId,
            RegistrarVistoriaRequest request,
            int usuarioId)
        {
            var vistoria = await _context.Vistorias
                .Include(v => v.RegistradoPor)
                .FirstOrDefaultAsync(v => v.Id == vistoriaId)
                ?? throw new InvalidOperationException(
                    $"Vistoria {vistoriaId} não encontrada. Use o endpoint de criação (POST).");

            if (request.HorarioTermino <= request.HorarioInicio)
                throw new InvalidOperationException("O horário de término deve ser posterior ao horário de início.");

            vistoria.DataVistoria = request.DataVistoria;
            vistoria.HorarioInicio = request.HorarioInicio;
            vistoria.HorarioTermino = request.HorarioTermino;
            vistoria.DescricaoDoLocal = request.DescricaoDoLocal;
            vistoria.CaracterizacaoDoLocal = request.CaracterizacaoDoLocal;
            vistoria.Edificacao = request.Edificacao;
            vistoria.Estrutura = request.Estrutura;
            vistoria.NumeroMoradias = request.NumeroMoradias;
            vistoria.NumeroComodos = request.NumeroComodos;
            vistoria.NumeroPavimentos = request.NumeroPavimentos;
            vistoria.NumeroMoradiasNoLote = request.NumeroMoradiasNoLote;
            vistoria.PossuiUnidadeFamiliar = request.PossuiUnidadeFamiliar;
            vistoria.NumeroAdultos = request.NumeroAdultos;
            vistoria.NumeroCriancas = request.NumeroCriancas;
            vistoria.NumeroIdosos = request.NumeroIdosos;
            vistoria.NumeroDeficientes = request.NumeroDeficientes;
            vistoria.TotalMoradores = request.TotalMoradores
                ?? (request.NumeroAdultos + request.NumeroCriancas + request.NumeroIdosos + request.NumeroDeficientes);
            vistoria.TipoRisco = request.TipoRisco;
            vistoria.GrauRiscoEncontrado = request.GrauRiscoEncontrado;
            vistoria.TipificacaoOcorrencia = request.TipificacaoOcorrencia;
            vistoria.RegimeOcupacao = request.RegimeOcupacao;
            vistoria.Motivacao = request.Motivacao;
            vistoria.AreasAfetadas = request.AreasAfetadas;
            vistoria.Interdicao = request.Interdicao;
            vistoria.Remocao = request.Remocao;
            vistoria.Orientacoes = request.Orientacoes;
            vistoria.EncaminhamentosDeCampo = request.EncaminhamentosDeCampo;
            vistoria.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Vistoria {VistoriaId} atualizada", vistoriaId);

            return MapearVistoriaDto(vistoria);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // FOTOS DE CAMPO (FOTO_CAMPO → Fotos/Fotos_da_Vistoria)
        // ═══════════════════════════════════════════════════════════════════════════

        public async Task<int> AdicionarFotosCampoAsync(
            int ocorrenciaId,
            int vistoriaId,
            List<IFormFile> fotos,
            int usuarioId)
        {
            // Valida a vistoria e obtém o protocolo da ocorrência
            var vistoria = await _context.Vistorias
                .Include(v => v.Ocorrencia)
                .FirstOrDefaultAsync(v => v.Id == vistoriaId && v.OcorrenciaId == ocorrenciaId)
                ?? throw new InvalidOperationException(
                    $"Vistoria {vistoriaId} não encontrada para a ocorrência {ocorrenciaId}.");

            var protocolo = vistoria.Ocorrencia.Protocolo;

            var arquivosParaUpload = new List<(Stream FileStream, string FileName, TipoArquivo TipoArquivo)>();

            foreach (var foto in fotos)
            {
                var extensao = Path.GetExtension(foto.FileName);
                var nomeUnico = $"{TipoArquivo.FOTO_CAMPO}_{Guid.NewGuid()}{extensao}";
                var ms = new MemoryStream();
                await foto.CopyToAsync(ms);
                ms.Position = 0;
                arquivosParaUpload.Add((ms, nomeUnico, TipoArquivo.FOTO_CAMPO));
            }

            List<string> caminhos;
            try
            {
                // CriarEstruturaPastasAsync é idempotente — não recria pastas existentes
                await _storageService.CriarEstruturaPastasAsync(protocolo);
                caminhos = await _storageService.SalvarArquivosAsync(protocolo, arquivosParaUpload);
            }
            finally
            {
                foreach (var (stream, _, _) in arquivosParaUpload)
                    await stream.DisposeAsync();
            }

            foreach (var (caminhoRelativo, foto) in caminhos.Zip(fotos))
            {
                _context.Arquivos.Add(new Arquivo
                {
                    OcorrenciaId = ocorrenciaId,
                    NomeOriginal = foto.FileName,
                    TipoArquivo = TipoArquivo.FOTO_CAMPO.ToString(),
                    CaminhoRelativo = caminhoRelativo,
                    TamanhoBytes = foto.Length,
                    EnviadoPorUserId = usuarioId,
                    EnviadoEm = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "{Count} foto(s) de campo adicionadas à vistoria {VistoriaId} (ocorrência {Protocolo})",
                caminhos.Count, vistoriaId, protocolo);

            return caminhos.Count;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task ValidarVistoriadorAsync(int vistoriadorId, int numero)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == vistoriadorId && u.Ativo)
                ?? throw new InvalidOperationException(
                    $"Vistoriador {numero} (ID {vistoriadorId}) não encontrado ou inativo.");

            if (usuario.TipoUsuario != TipoUsuario.VISTORIADOR && usuario.TipoUsuario != TipoUsuario.ADMIN)
                throw new InvalidOperationException(
                    $"Usuário {vistoriadorId} não tem permissão de vistoriador (tipo: {usuario.TipoUsuario}).");
        }

        private async Task<AgendamentoVistoriaDto> ObterAgendamentoDtoAsync(int agendamentoId)
        {
            var agendamento = await _context.AgendamentosVistoria
                .Include(a => a.Vistoriador1)
                .Include(a => a.Vistoriador2)
                .Include(a => a.AgendadoPor)
                .Include(a => a.Tentativas)
                .FirstAsync(a => a.Id == agendamentoId);

            return MapearAgendamentoDto(agendamento);
        }

        private static AgendamentoVistoriaDto MapearAgendamentoDto(AgendamentoVistoria a) => new()
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
        };

        private static VistoriaDto MapearVistoriaDto(Data.Entities.Tabelas.Ocorrencia.Vistoria v) => new()
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
        };
    }
}
