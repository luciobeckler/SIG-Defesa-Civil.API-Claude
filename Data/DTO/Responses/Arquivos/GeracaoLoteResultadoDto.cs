namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos
{
    /// <summary>
    /// Resultado da geração de documentos em lote
    /// </summary>
    public class GeracaoLoteResultadoDto
    {
        public int TotalProcessados { get; set; }
        public int Sucessos { get; set; }
        public int Falhas { get; set; }
        public List<DocumentoGeradoDto> Detalhes { get; set; } = new();
    }
}
