namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos
{
    /// <summary>
    /// Informações sobre um documento gerado
    /// </summary>
    public class DocumentoGeradoDto
    {
        public int OcorrenciaId { get; set; }
        public string Protocolo { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
        public string? MensagemErro { get; set; }
        public string? SharePointUrl { get; set; }
        public DateTime? GeradoEm { get; set; }
    }
}
