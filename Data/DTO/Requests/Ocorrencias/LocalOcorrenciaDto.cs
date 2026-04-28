namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    public class LocalOcorrenciaDto
    {
        public string EnderecoCompleto { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
