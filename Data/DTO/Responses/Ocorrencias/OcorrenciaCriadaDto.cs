using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    public class OcorrenciaCriadaDto
    {
        public int Id { get; set; }
        public string Protocolo { get; set; } = string.Empty;
        public DateTime AbertaEm { get; set; }
        public StatusOcorrencia Status { get; set; }
        public int ArquivosSalvos { get; set; }
    }
}
