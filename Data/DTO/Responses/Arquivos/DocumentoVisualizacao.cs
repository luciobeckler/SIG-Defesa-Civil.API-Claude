using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos
{
    public class DocumentoVisualizacao
    {
        public string NomeOriginal { get; set; } = string.Empty;
        public TipoArquivo TipoArquivo { get; set; }
        public string CaminhoRelativo { get; set; } = string.Empty;
        public long TamanhoBytes { get; set; }
        public int EnviadoPorUserId { get; set; }
        public DateTime EnviadoEm { get; set; }
    }
}
