namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos
{
    /// <summary>
    /// DTO usado na Central de Documentos (GET /ocorrencias/{id}/arquivos).
    /// Inclui o Id do registro para operações futuras (exclusão, auditoria).
    /// </summary>
    public class ArquivoListagemDto
    {
        public int    Id               { get; set; }

        /// <summary>Categoria do arquivo (string enum: FOTO_CIDADAO, FOTO_CAMPO, etc.)</summary>
        public string TipoArquivo      { get; set; } = string.Empty;

        public string NomeOriginal     { get; set; } = string.Empty;

        /// <summary>
        /// Caminho relativo utilizado para download via
        /// GET /ocorrencias/{id}/arquivos/download?caminho={CaminhoRelativo}
        /// </summary>
        public string CaminhoRelativo  { get; set; } = string.Empty;

        public long   TamanhoBytes     { get; set; }

        /// <summary>Nome do usuário que fez o upload. Null quando o remetente foi removido.</summary>
        public string? EnviadoPor      { get; set; }

        public DateTime EnviadoEm      { get; set; }
    }
}
