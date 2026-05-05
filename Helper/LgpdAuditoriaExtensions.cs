using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Helper
{
    /// <summary>
    /// Extension methods para facilitar registro de auditoria LGPD
    /// </summary>
    public static class LgpdAuditoriaExtensions
    {
        /// <summary>
        /// Registra um acesso no log LGPD de forma assíncrona
        /// </summary>
        public static async Task RegistrarAcessoLgpdAsync(
            this DefesaCivilContext context,
            int usuarioId,
            AcaoLgpd acao,
            int? ocorrenciaId = null,
            int? arquivoId = null,
            string? ipOrigem = null,
            string? userAgent = null)
        {
            var log = new LogAcessoLgpd
            {
                UsuarioId = usuarioId,
                OcorrenciaId = ocorrenciaId,
                ArquivoId = arquivoId,
                Acao = acao,
                IpOrigem = ipOrigem,
                UserAgent = userAgent,
                RegistradoEm = DateTime.UtcNow
            };

            context.LogsLgpd.Add(log);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Busca histórico de acessos a uma ocorrência específica
        /// </summary>
        public static async Task<List<LogAcessoLgpd>> ObterHistoricoAcessosAsync(
            this DefesaCivilContext context,
            int ocorrenciaId,
            int? limite = 50)
        {
            return await context.LogsLgpd
                .Include(l => l.Usuario)
                .Where(l => l.OcorrenciaId == ocorrenciaId)
                .OrderByDescending(l => l.RegistradoEm)
                .Take(limite ?? 50)
                .ToListAsync();
        }

        /// <summary>
        /// Conta quantas vezes um usuário acessou dados sensíveis (auditoria)
        /// </summary>
        public static async Task<int> ContarAcessosUsuarioAsync(
            this DefesaCivilContext context,
            int usuarioId,
            DateTime? dataInicio = null,
            DateTime? dataFim = null)
        {
            var query = context.LogsLgpd
                .Where(l => l.UsuarioId == usuarioId && l.Acao == AcaoLgpd.VISUALIZOU);

            if (dataInicio.HasValue)
                query = query.Where(l => l.RegistradoEm >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(l => l.RegistradoEm <= dataFim.Value);

            return await query.CountAsync();
        }
    }
}
