namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Filtros opcionais para listagem de ocorrências
    /// </summary>
    public class FiltroOcorrenciaDto
    {
        public string? Status { get; set; }
        public string? NivelGravidade { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public int? VistoriadorId { get; set; }
        public string? Protocolo { get; set; }
    }
}
