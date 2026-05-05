using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Services.Notificacao
{
    public class NotificacaoService : INotificacaoService
    {
        private readonly DefesaCivilContext _context;
        private readonly ILogger<NotificacaoService> _logger;

        public NotificacaoService(DefesaCivilContext context, ILogger<NotificacaoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<NotificadoDto>> RegistrarAsync(
            int ocorrenciaId,
            RegistrarNotificadosRequest request,
            int usuarioId)
        {
            var ocorrencia = await _context.Ocorrencias
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            if (ocorrencia.Status != StatusOcorrencia.VISTORIA_REALIZADA &&
                ocorrencia.Status != StatusOcorrencia.NOTIFICADA)
                throw new InvalidOperationException(
                    $"Os notificados só podem ser registrados quando a ocorrência está em " +
                    $"VISTORIA_REALIZADA ou NOTIFICADA. Status atual: {ocorrencia.Status}.");

            var novosNotificados = request.Notificados.Select(item => new Notificado
            {
                OcorrenciaId = ocorrenciaId,
                Nome = item.Nome,
                RgCpf = item.RgCpf,
                DataNotificacao = item.DataNotificacao,
                RegistradoPorId = usuarioId,
                RegistradoEm = DateTime.UtcNow
            }).ToList();

            _context.Notificados.AddRange(novosNotificados);

            if (ocorrencia.Status == StatusOcorrencia.VISTORIA_REALIZADA)
            {
                ocorrencia.Status = StatusOcorrencia.NOTIFICADA;
                ocorrencia.AtualizadoEm = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "{Count} notificado(s) registrado(s) para ocorrência {Protocolo} → status NOTIFICADA",
                novosNotificados.Count, ocorrencia.Protocolo);

            // Recarregar com navegação
            foreach (var n in novosNotificados)
                await _context.Entry(n).Reference(x => x.RegistradoPor).LoadAsync();

            return novosNotificados.Select(MapearDto).ToList();
        }

        public async Task<List<NotificadoDto>> ListarPorOcorrenciaAsync(int ocorrenciaId)
        {
            var notificados = await _context.Notificados
                .Include(n => n.RegistradoPor)
                .Where(n => n.OcorrenciaId == ocorrenciaId)
                .OrderBy(n => n.RegistradoEm)
                .ToListAsync();

            return notificados.Select(MapearDto).ToList();
        }

        public async Task RemoverNotificadoAsync(int notificadoId, int usuarioId)
        {
            var notificado = await _context.Notificados
                .Include(n => n.Ocorrencia)
                .FirstOrDefaultAsync(n => n.Id == notificadoId)
                ?? throw new InvalidOperationException($"Notificado {notificadoId} não encontrado.");

            var ocorrencia = notificado.Ocorrencia;
            _context.Notificados.Remove(notificado);
            await _context.SaveChangesAsync();

            // Se não há mais notificados, reverter status para VISTORIA_REALIZADA
            var aindarestam = await _context.Notificados.AnyAsync(n => n.OcorrenciaId == ocorrencia.Id);
            if (!aindarestam && ocorrencia.Status == StatusOcorrencia.NOTIFICADA)
            {
                ocorrencia.Status = StatusOcorrencia.VISTORIA_REALIZADA;
                ocorrencia.AtualizadoEm = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Último notificado removido. Ocorrência {Protocolo} revertida para VISTORIA_REALIZADA",
                    ocorrencia.Protocolo);
            }
        }

        // ── Helper ───────────────────────────────────────────────────────────────

        private static NotificadoDto MapearDto(Notificado n) => new()
        {
            Id = n.Id,
            Nome = n.Nome,
            RgCpf = n.RgCpf,
            DataNotificacao = n.DataNotificacao,
            RegistradoPor = n.RegistradoPor.Nome,
            RegistradoEm = n.RegistradoEm
        };
    }
}
