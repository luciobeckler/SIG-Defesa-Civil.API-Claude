using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Models.Tabelas
{
    [Table("ocorrencias")]
    public class Ocorrencia
    {
        public int Id { get; set; }
        public string Protocolo { get; set; } = null!; // YYYY-XXXX
        public int CidadaoId { get; set; }
        public Usuario Cidadao { get; set; } = null!;

        public string EnderecoCompleto { get; set; } = null!;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string? TipoRisco { get; set; }
        public string? NivelGravidade { get; set; }
        public StatusOcorrencia Status { get; set; } = StatusOcorrencia.ABERTA;

        public int? AtendenteId { get; set; }
        public Usuario? Atendente { get; set; }
        public int? VistoriadorId { get; set; }
        public Usuario? Vistoriador { get; set; }

        public DateTime AbertaEm { get; set; } = DateTime.UtcNow;
        public DateTime? TriagemEm { get; set; }
        public DateTime? VistoriaEm { get; set; }
        public DateTime? ConcluidaEm { get; set; }

        public ICollection<Arquivo> Arquivos { get; set; } = new List<Arquivo>();
    }
}
