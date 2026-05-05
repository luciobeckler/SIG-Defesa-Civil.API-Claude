namespace SIG_Defesa_Civil.API.Services.Documento
{
    /// <summary>
    /// Serviço especializado em geração de documentos Word a partir de templates
    /// </summary>
    public interface IDocumentoService
    {
        /// <summary>
        /// Preenche um template .docx substituindo tags no formato &lt;&lt;CAMPO&gt;&gt; pelos valores fornecidos.
        /// Lida de forma segura com quebras internas de XML que o Word pode gerar.
        /// </summary>
        /// <param name="templateStream">Stream do arquivo .docx template</param>
        /// <param name="dados">Dicionário com os dados para substituição (chave = nome do campo, valor = texto)</param>
        /// <returns>Stream do documento preenchido, pronto para upload</returns>
        /// <exception cref="InvalidOperationException">Se o template for inválido ou houver erro na substituição</exception>
        Task<Stream> PreencherTemplateAsync(
            Stream templateStream,
            Dictionary<string, string> dados);

        /// <summary>
        /// Valida se um template possui todas as tags esperadas
        /// </summary>
        /// <param name="templateStream">Stream do arquivo .docx template</param>
        /// <returns>Lista de tags encontradas no formato &lt;&lt;CAMPO&gt;&gt;</returns>
        Task<List<string>> ExtrairTagsDoTemplateAsync(Stream templateStream);
    }
}
