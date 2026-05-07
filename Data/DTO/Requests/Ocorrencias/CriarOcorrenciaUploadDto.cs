namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    public class CriarOcorrenciaUploadDto
    {
        public string Dados { get; set; }
        public IFormFile Comprovante { get; set; }
        public List<IFormFile>? Fotos { get; set; }
    }
}
