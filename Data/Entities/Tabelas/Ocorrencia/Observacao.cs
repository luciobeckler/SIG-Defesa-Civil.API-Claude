using SIG_Defesa_Civil.API.Data.Models.Tabelas;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    public class Observacao
    {
        public int Id { get; set; }
        public int OcorrenciaId { get; set; }
        public int UsuarioId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }

        // Navegação
        public Ocorrencia Ocorrencia { get; set; } = null!;
        public Usuario Usuario { get; set; } = null!;
    }
}
