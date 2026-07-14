using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Services.Storage;

namespace SIG_Defesa_Civil.API.Services.Notificacao
{
    public class NotificacaoService : INotificacaoService
    {
        private readonly DefesaCivilContext _context;
        private readonly IStorageService _storageService;
        private readonly ILogger<NotificacaoService> _logger;

        public NotificacaoService(
            DefesaCivilContext context,
            IStorageService storageService,
            ILogger<NotificacaoService> logger)
        {
            _context = context;
            _storageService = storageService;
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

            // Notificados são uma propriedade da ocorrência (quem recebeu o relatório),
            // não uma etapa: podem ser registrados a qualquer momento e não alteram o status.
            if (ocorrencia.Status == StatusOcorrencia.CANCELADA)
                throw new InvalidOperationException(
                    "Não é possível registrar notificados em uma ocorrência cancelada.");

            var novosNotificados = request.Notificados.Select(item => new Notificado
            {
                OcorrenciaId = ocorrenciaId,
                Nome = item.Nome,
                RgCpf = item.RgCpf,
                DataNotificacao = item.DataNotificacao,
                FormaRecebimento = item.FormaRecebimento,
                RegistradoPorId = usuarioId,
                RegistradoEm = DateTime.UtcNow
            }).ToList();

            _context.Notificados.AddRange(novosNotificados);

            ocorrencia.AtualizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "{Count} notificado(s) registrado(s) para ocorrência {Protocolo}",
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

            _logger.LogInformation(
                "Notificado {NotificadoId} removido da ocorrência {Protocolo} pelo usuário {UsuarioId}",
                notificadoId, ocorrencia.Protocolo, usuarioId);
        }

        public async Task SalvarAssinaturaNotificadoAsync(
            int ocorrenciaId, int notificadoId, IFormFile arquivo, int usuarioId)
        {
            var notificado = await _context.Notificados
                .Include(n => n.Ocorrencia)
                .FirstOrDefaultAsync(n => n.Id == notificadoId && n.OcorrenciaId == ocorrenciaId)
                ?? throw new InvalidOperationException(
                    $"Notificado {notificadoId} não encontrado para a ocorrência {ocorrenciaId}.");

            var protocolo = notificado.Ocorrencia.Protocolo;

            // Nome determinístico: uma assinatura por notificado (substituição)
            var nomeArquivo = $"assinatura_notificado_{notificadoId}.png";

            var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms);
            ms.Position = 0;

            string caminho;
            try
            {
                await _storageService.CriarEstruturaPastasAsync(protocolo);
                caminho = await _storageService.SalvarArquivoAsync(
                    protocolo, nomeArquivo, TipoArquivo.ASSINATURA_MUNICIPIO, ms);
            }
            finally
            {
                await ms.DisposeAsync();
            }

            var anterior = await _context.Arquivos.FirstOrDefaultAsync(a =>
                a.OcorrenciaId == ocorrenciaId &&
                a.TipoArquivo  == TipoArquivo.ASSINATURA_MUNICIPIO.ToString() &&
                a.NomeOriginal == nomeArquivo);
            if (anterior != null)
                _context.Arquivos.Remove(anterior);

            _context.Arquivos.Add(new Arquivo
            {
                OcorrenciaId     = ocorrenciaId,
                NomeOriginal     = nomeArquivo,
                TipoArquivo      = TipoArquivo.ASSINATURA_MUNICIPIO.ToString(),
                CaminhoRelativo  = caminho,
                TamanhoBytes     = arquivo.Length,
                EnviadoPorUserId = usuarioId,
                EnviadoEm        = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Assinatura do notificado {NotificadoId} salva para ocorrência {Protocolo}",
                notificadoId, protocolo);
        }

        // ── Helper ───────────────────────────────────────────────────────────────

        private static NotificadoDto MapearDto(Notificado n) => new()
        {
            Id = n.Id,
            Nome = n.Nome,
            RgCpf = n.RgCpf,
            DataNotificacao = n.DataNotificacao,
            FormaRecebimento = n.FormaRecebimento.ToString(),
            RegistradoPor = n.RegistradoPor.Nome,
            RegistradoEm = n.RegistradoEm
        };
    }
}
