using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Pessoa notificada na Etapa 5 do fluxo.
    /// Relacionamento 1:N com Ocorrencia — cada notificado é uma linha separada,
    /// evitando o anti-pattern de colunas NOTIFICADO_1 / NOTIFICADO_2 da planilha.
    /// </summary>
    [Table("notificados")]
    public class Notificado
    {
        public int Id { get; set; }

        // FK (muitos notificados por ocorrência — 1:N)
        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

        // ── Dados do notificado ──────────────────────────────────────────────────
        public string Nome { get; set; } = null!;

        /// <summary>RG ou CPF do notificado (campo unificado conforme planilha).</summary>
        public string? RgCpf { get; set; }

        public DateOnly DataNotificacao { get; set; }

        // ── Auditoria ────────────────────────────────────────────────────────────
        public int RegistradoPorId { get; set; }
        public Usuario RegistradoPor { get; set; } = null!;
        public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
    }
}
