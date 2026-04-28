using SIG_Defesa_Civil.API.Data.DTO.Responses.Usuairos;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>
    /// DTO para listagem de ocorrências com dados sensíveis mascarados (LGPD)
    /// </summary>
    public class OcorrenciaListagemDto
    {
        public int Id { get; set; }
        public string Protocolo { get; set; } = string.Empty;

        // Dados do cidadão (mascarados)
        public CidadaoMascaradoDto Cidadao { get; set; } = null!;

        // Dados do local
        public string EnderecoCompleto { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Classificação
        public string? TipoRisco { get; set; }
        public string? NivelGravidade { get; set; }

        // Status operacional
        public string Status { get; set; } = string.Empty;

        // Atribuições
        public string? NomeAtendente { get; set; }
        public string? NomeVistoriador { get; set; }

        // Timestamps
        public DateTime AbertaEm { get; set; }
        public DateTime? TriagemEm { get; set; }
        public DateTime? VistoriaEm { get; set; }
        public DateTime? ConcluidaEm { get; set; }

        // Estatísticas
        public int QuantidadeArquivos { get; set; }
    }
}
