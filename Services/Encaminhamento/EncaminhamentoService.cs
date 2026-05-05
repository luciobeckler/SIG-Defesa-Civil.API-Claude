using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Services.Encaminhamento
{
    public class EncaminhamentoService : IEncaminhamentoService
    {
        private readonly DefesaCivilContext _context;
        private readonly ILogger<EncaminhamentoService> _logger;

        public EncaminhamentoService(DefesaCivilContext context, ILogger<EncaminhamentoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EncaminhamentoFinalDto> RegistrarAsync(
            int ocorrenciaId,
            RegistrarEncaminhamentoRequest request,
            int usuarioId)
        {
            var ocorrencia = await _context.Ocorrencias
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            if (ocorrencia.Status != StatusOcorrencia.NOTIFICADA)
                throw new InvalidOperationException(
                    $"O encaminhamento final só pode ser registrado quando a ocorrência está NOTIFICADA. " +
                    $"Status atual: {ocorrencia.Status}.");

            var jaExiste = await _context.EncaminhamentosFinais.AnyAsync(e => e.OcorrenciaId == ocorrenciaId);
            if (jaExiste)
                throw new InvalidOperationException(
                    "Esta ocorrência já possui um encaminhamento final. Use o endpoint de atualização (PUT).");

            if (request.RelatorioVistoriaId.HasValue)
                await ValidarArquivoRelatorioAsync(request.RelatorioVistoriaId.Value);

            var encaminhamento = new EncaminhamentoFinal
            {
                OcorrenciaId = ocorrenciaId,
                Encaminhamentos = request.Encaminhamentos,
                RetornoEncaminhamentos = request.RetornoEncaminhamentos,
                RelatorioVistoriaId = request.RelatorioVistoriaId,
                EntregaRelatorio = request.EntregaRelatorio,
                RegistradoPorId = usuarioId,
                RegistradoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };

            _context.EncaminhamentosFinais.Add(encaminhamento);

            ocorrencia.Status = StatusOcorrencia.ENCERRADA;
            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Encaminhamento registrado. Ocorrência {Protocolo} → status ENCERRADA",
                ocorrencia.Protocolo);

            return await ObterDtoCompletoAsync(encaminhamento.Id);
        }

        public async Task<EncaminhamentoFinalDto?> ObterPorOcorrenciaAsync(int ocorrenciaId)
        {
            var encaminhamento = await _context.EncaminhamentosFinais
                .Include(e => e.RegistradoPor)
                .Include(e => e.RelatorioVistoria)
                .FirstOrDefaultAsync(e => e.OcorrenciaId == ocorrenciaId);

            return encaminhamento == null ? null : MapearDto(encaminhamento);
        }

        public async Task<EncaminhamentoFinalDto> AtualizarAsync(
            int ocorrenciaId,
            RegistrarEncaminhamentoRequest request,
            int usuarioId)
        {
            var encaminhamento = await _context.EncaminhamentosFinais
                .Include(e => e.RegistradoPor)
                .Include(e => e.RelatorioVistoria)
                .FirstOrDefaultAsync(e => e.OcorrenciaId == ocorrenciaId)
                ?? throw new InvalidOperationException(
                    $"Nenhum encaminhamento encontrado para a ocorrência {ocorrenciaId}. Use o endpoint de criação (POST).");

            if (request.RelatorioVistoriaId.HasValue &&
                request.RelatorioVistoriaId != encaminhamento.RelatorioVistoriaId)
                await ValidarArquivoRelatorioAsync(request.RelatorioVistoriaId.Value);

            encaminhamento.Encaminhamentos = request.Encaminhamentos;
            encaminhamento.RetornoEncaminhamentos = request.RetornoEncaminhamentos;
            encaminhamento.RelatorioVistoriaId = request.RelatorioVistoriaId;
            encaminhamento.EntregaRelatorio = request.EntregaRelatorio;
            encaminhamento.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recarregar FK de arquivo se mudou
            if (encaminhamento.RelatorioVistoriaId.HasValue)
                await _context.Entry(encaminhamento).Reference(e => e.RelatorioVistoria).LoadAsync();

            _logger.LogInformation("Encaminhamento atualizado. Ocorrência ID {OcorrenciaId}", ocorrenciaId);

            return MapearDto(encaminhamento);
        }

        public async Task ReabrirAsync(int ocorrenciaId, int usuarioId, string motivo)
        {
            var ocorrencia = await _context.Ocorrencias
                .Where(o => o.DeletedAt == null && o.Status == StatusOcorrencia.ENCERRADA)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException(
                    $"Ocorrência {ocorrenciaId} não encontrada ou não está ENCERRADA.");

            ocorrencia.Status = StatusOcorrencia.NOTIFICADA;
            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Ocorrência {Protocolo} REABERTA pelo usuário {UsuarioId}. Motivo: {Motivo}",
                ocorrencia.Protocolo, usuarioId, motivo);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task ValidarArquivoRelatorioAsync(int arquivoId)
        {
            var arquivo = await _context.Arquivos.FirstOrDefaultAsync(a => a.Id == arquivoId)
                ?? throw new InvalidOperationException($"Arquivo {arquivoId} não encontrado.");

            if (arquivo.TipoArquivo != TipoArquivo.RELATORIO_FINAL.ToString())
                throw new InvalidOperationException(
                    $"O arquivo {arquivoId} não é do tipo RELATORIO_FINAL (tipo atual: {arquivo.TipoArquivo}).");
        }

        private async Task<EncaminhamentoFinalDto> ObterDtoCompletoAsync(int encaminhamentoId)
        {
            var encaminhamento = await _context.EncaminhamentosFinais
                .Include(e => e.RegistradoPor)
                .Include(e => e.RelatorioVistoria)
                .FirstAsync(e => e.Id == encaminhamentoId);

            return MapearDto(encaminhamento);
        }

        private static EncaminhamentoFinalDto MapearDto(EncaminhamentoFinal e) => new()
        {
            Id = e.Id,
            Encaminhamentos = e.Encaminhamentos,
            RetornoEncaminhamentos = e.RetornoEncaminhamentos,
            EntregaRelatorio = e.EntregaRelatorio,
            RegistradoPor = e.RegistradoPor.Nome,
            RegistradoEm = e.RegistradoEm,
            AtualizadoEm = e.AtualizadoEm,
            RelatorioVistoria = e.RelatorioVistoria == null ? null : new DocumentoVisualizacao
            {
                NomeOriginal = e.RelatorioVistoria.NomeOriginal,
                TipoArquivo = Enum.Parse<TipoArquivo>(e.RelatorioVistoria.TipoArquivo),
                CaminhoRelativo = e.RelatorioVistoria.CaminhoRelativo,
                TamanhoBytes = e.RelatorioVistoria.TamanhoBytes,
                EnviadoPorUserId = e.RelatorioVistoria.EnviadoPorUserId,
                EnviadoEm = e.RelatorioVistoria.EnviadoEm
            }
        };
    }
}
