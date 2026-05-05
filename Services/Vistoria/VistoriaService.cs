using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Services.Vistoria
{
    public class VistoriaService : IVistoriaService
    {
        private readonly DefesaCivilContext _context;
        private readonly ILogger<VistoriaService> _logger;

        private const int MaxTentativas = 3;

        public VistoriaService(DefesaCivilContext context, ILogger<VistoriaService> logger)
        {
            _context = context;
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

            if (ocorrencia.Status != StatusOcorrencia.EM_AVALIACAO)
                throw new InvalidOperationException(
                    $"O agendamento só pode ser criado quando a ocorrência está EM_AVALIACAO. " +
                    $"Status atual: {ocorrencia.Status}.");

            var jaExiste = await _context.AgendamentosVistoria.AnyAsync(a => a.OcorrenciaId == ocorrenciaId);
            if (jaExiste)
                throw new InvalidOperationException("Esta ocorrência já possui um agendamento de vistoria.");

            if (request.Vistoriador2Id.HasValue && request.Vistoriador2Id == request.Vistoriador1Id)
                throw new InvalidOperationException("O vistoriador 1 e o vistoriador 2 não podem ser o mesmo usuário.");

            await ValidarVistoriadorAsync(request.Vistoriador1Id, 1);
            if (request.Vistoriador2Id.HasValue)
                await ValidarVistoriadorAsync(request.Vistoriador2Id.Value, 2);

            var agendamento = new AgendamentoVistoria
            {
                OcorrenciaId = ocorrenciaId,
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

            ocorrencia.Status = StatusOcorrencia.VISTORIA_SOLICITADA;
            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Agendamento criado. Ocorrência {Protocolo} → status VISTORIA_SOLICITADA. " +
                "Vistoriadores: {V1} / {V2}",
                ocorrencia.Protocolo, request.Vistoriador1Id, request.Vistoriador2Id?.ToString() ?? "-");

            return await ObterAgendamentoDtoAsync(agendamento.Id);
        }

        public async Task<AgendamentoVistoriaDto?> ObterAgendamentoPorOcorrenciaAsync(int ocorrenciaId)
        {
            var agendamento = await _context.AgendamentosVistoria
                .Include(a => a.Vistoriador1)
                .Include(a => a.Vistoriador2)
                .Include(a => a.AgendadoPor)
                .Include(a => a.Tentativas)
                .FirstOrDefaultAsync(a => a.OcorrenciaId == ocorrenciaId);

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

            // Recarregar tentativas atualizadas
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

            var jaExiste = await _context.Vistorias.AnyAsync(v => v.OcorrenciaId == ocorrenciaId);
            if (jaExiste)
                throw new InvalidOperationException(
                    "Esta ocorrência já possui uma vistoria registrada. Use o endpoint de atualização (PUT).");

            if (request.HorarioTermino <= request.HorarioInicio)
                throw new InvalidOperationException("O horário de término deve ser posterior ao horário de início.");

            var totalMoradores = request.TotalMoradores
                ?? (request.NumeroAdultos + request.NumeroCriancas + request.NumeroIdosos + request.NumeroDeficientes);

            var vistoria = new Data.Entities.Tabelas.Ocorrencia.Vistoria
            {
                OcorrenciaId = ocorrenciaId,
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

            ocorrencia.Status = StatusOcorrencia.VISTORIA_REALIZADA;
            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Vistoria registrada. Ocorrência {Protocolo} → status VISTORIA_REALIZADA",
                ocorrencia.Protocolo);

            await _context.Entry(vistoria).Reference(v => v.RegistradoPor).LoadAsync();
            return MapearVistoriaDto(vistoria);
        }

        public async Task<VistoriaDto?> ObterVistoriaPorOcorrenciaAsync(int ocorrenciaId)
        {
            var vistoria = await _context.Vistorias
                .Include(v => v.RegistradoPor)
                .FirstOrDefaultAsync(v => v.OcorrenciaId == ocorrenciaId);

            return vistoria == null ? null : MapearVistoriaDto(vistoria);
        }

        public async Task<VistoriaDto> AtualizarVistoriaAsync(
            int ocorrenciaId,
            RegistrarVistoriaRequest request,
            int usuarioId)
        {
            var vistoria = await _context.Vistorias
                .Include(v => v.RegistradoPor)
                .FirstOrDefaultAsync(v => v.OcorrenciaId == ocorrenciaId)
                ?? throw new InvalidOperationException(
                    $"Nenhuma vistoria encontrada para a ocorrência {ocorrenciaId}. Use o endpoint de criação (POST).");

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

            _logger.LogInformation("Vistoria atualizada. Ocorrência ID {OcorrenciaId}", ocorrenciaId);

            return MapearVistoriaDto(vistoria);
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
