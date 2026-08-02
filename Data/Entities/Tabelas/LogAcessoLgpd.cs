using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Models.Tabelas
{
    [Table("log_acesso_lgpd")]
    public class LogAcessoLgpd
    {
        public int Id { get; set; }

        /// <summary>
        /// Colaborador responsável pela ação. Nulo quando a ação partiu do portal
        /// público (abertura pelo cidadão), onde não há usuário autenticado.
        /// </summary>
        public int? UsuarioId { get; set; }
        public int? OcorrenciaId { get; set; }
        public int? ArquivoId { get; set; }

        public AcaoLgpd Acao { get; set; }
        public string? IpOrigem { get; set; }
        public string? UserAgent { get; set; }
        public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;

        // Navegação
        public Usuario? Usuario { get; set; }
        public Ocorrencia? Ocorrencia { get; set; }
        public Arquivo? Arquivo { get; set; }
    }
}
