using SIG_Defesa_Civil.API.Data.DTO.Responses.Usuairos;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>
    /// DTO com dados sensíveis revelados (requer log LGPD)
    /// Retornado apenas após chamada explícita ao endpoint de revelação
    /// </summary>
    public class OcorrenciaDadosSensiveisDto
    {
        public int Id { get; set; }
        public string Protocolo { get; set; } = string.Empty;

        // Dados completos do cidadão (SEM mascaramento)
        public CidadaoCompletoDto Cidadao { get; set; } = null!;

        // Dados do local
        public string EnderecoCompleto { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Classificação
        public string? TipoRisco { get; set; }
        public string? NivelGravidade { get; set; }
        public string Status { get; set; } = string.Empty;

        // Metadados de acesso (transparência LGPD)
        public AcessoLgpdDto UltimoAcesso { get; set; } = null!;
    }
}
