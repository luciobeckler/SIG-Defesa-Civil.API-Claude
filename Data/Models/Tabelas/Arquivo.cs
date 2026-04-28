using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Models.Tabelas
{
    [Table("arquivos")]
    public class Arquivo
    {
        public int Id { get; set; }
        public int OcorrenciaId { get; set; }

        public string NomeOriginal { get; set; } = null!;
        public TipoArquivo TipoArquivo { get; set; }

        public string SharepointId { get; set; } = null!;
        public string SharepointUrl { get; set; } = null!;

        public int EnviadoPor { get; set; }
        public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;

        // Navegação
        public Usuario UsuarioEnvio { get; set; } = null!;
        public Ocorrencia Ocorrencia { get; set; } = null!;

    }
}
