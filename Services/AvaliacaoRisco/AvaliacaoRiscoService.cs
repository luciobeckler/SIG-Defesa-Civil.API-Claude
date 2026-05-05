using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Services.AvaliacaoRisco
{
    public class AvaliacaoRiscoService : IAvaliacaoRiscoService
    {
        private readonly DefesaCivilContext _context;
        private readonly ILogger<AvaliacaoRiscoService> _logger;

        public AvaliacaoRiscoService(DefesaCivilContext context, ILogger<AvaliacaoRiscoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AvaliacaoRiscoDto> RegistrarAsync(
            int ocorrenciaId,
            RegistrarAvaliacaoRiscoRequest request,
            int usuarioId)
        {
            var ocorrencia = await _context.Ocorrencias
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            if (ocorrencia.Status != StatusOcorrencia.ABERTA)
                throw new InvalidOperationException(
                    $"A avaliação de risco só pode ser registrada quando a ocorrência está ABERTA. " +
                    $"Status atual: {ocorrencia.Status}.");

            var jaExiste = await _context.AvaliacoesRisco.AnyAsync(a => a.OcorrenciaId == ocorrenciaId);
            if (jaExiste)
                throw new InvalidOperationException(
                    "Esta ocorrência já possui uma avaliação de risco. Use o endpoint de atualização (PUT).");

            var avaliacao = new Data.Entities.Tabelas.Ocorrencia.AvaliacaoRisco
            {
                OcorrenciaId = ocorrenciaId,
                TipificacaoInicial = request.TipificacaoInicial,
                GrauRiscoInicial = request.GrauRiscoInicial,
                AbertaPorUsuarioId = request.AbertaPorUsuarioId > 0 ? request.AbertaPorUsuarioId : null,
                RequisicaoSetorDocumento = request.RequisicaoSetorDocumento,
                Emergencia = request.Emergencia,
                RegistradoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };

            _context.AvaliacoesRisco.Add(avaliacao);

            ocorrencia.Status = StatusOcorrencia.EM_AVALIACAO;
            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Avaliação de risco registrada. Ocorrência {Protocolo} → status EM_AVALIACAO",
                ocorrencia.Protocolo);

            return await ObterDtoCompletoAsync(avaliacao.Id);
        }

        public async Task<AvaliacaoRiscoDto?> ObterPorOcorrenciaAsync(int ocorrenciaId)
        {
            var avaliacao = await _context.AvaliacoesRisco
                .Include(a => a.AbertaPorUsuario)
                .FirstOrDefaultAsync(a => a.OcorrenciaId == ocorrenciaId);

            return avaliacao == null ? null : MapearDto(avaliacao);
        }

        public async Task<AvaliacaoRiscoDto> AtualizarAsync(
            int ocorrenciaId,
            RegistrarAvaliacaoRiscoRequest request,
            int usuarioId)
        {
            var avaliacao = await _context.AvaliacoesRisco
                .Include(a => a.AbertaPorUsuario)
                .FirstOrDefaultAsync(a => a.OcorrenciaId == ocorrenciaId)
                ?? throw new InvalidOperationException(
                    $"Nenhuma avaliação de risco encontrada para a ocorrência {ocorrenciaId}. " +
                    "Use o endpoint de criação (POST).");

            avaliacao.TipificacaoInicial = request.TipificacaoInicial;
            avaliacao.GrauRiscoInicial = request.GrauRiscoInicial;
            avaliacao.AbertaPorUsuarioId = request.AbertaPorUsuarioId > 0 ? request.AbertaPorUsuarioId : null;
            avaliacao.RequisicaoSetorDocumento = request.RequisicaoSetorDocumento;
            avaliacao.Emergencia = request.Emergencia;
            avaliacao.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Avaliação de risco atualizada. Ocorrência ID {OcorrenciaId}", ocorrenciaId);

            // Recarregar com navegação atualizada
            await _context.Entry(avaliacao).Reference(a => a.AbertaPorUsuario).LoadAsync();
            return MapearDto(avaliacao);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task<AvaliacaoRiscoDto> ObterDtoCompletoAsync(int avaliacaoId)
        {
            var avaliacao = await _context.AvaliacoesRisco
                .Include(a => a.AbertaPorUsuario)
                .FirstAsync(a => a.Id == avaliacaoId);

            return MapearDto(avaliacao);
        }

        private static AvaliacaoRiscoDto MapearDto(Data.Entities.Tabelas.Ocorrencia.AvaliacaoRisco a) => new()
        {
            Id = a.Id,
            TipificacaoInicial = a.TipificacaoInicial,
            GrauRiscoInicial = a.GrauRiscoInicial,
            NomeAgenteTriage = a.AbertaPorUsuario?.Nome,
            RequisicaoSetorDocumento = a.RequisicaoSetorDocumento,
            Emergencia = a.Emergencia,
            RegistradoEm = a.RegistradoEm,
            AtualizadoEm = a.AtualizadoEm
        };
    }
}
