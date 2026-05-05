namespace SIG_Defesa_Civil.API.Data.Configuration.DocumentTemplate
{
    /// <summary>
    /// Configurações para localização dos templates de documentos
    /// </summary>
    public class TemplateSettings
    {
        /// <summary>
        /// Caminho raiz onde os templates .docx estão armazenados no servidor
        /// Exemplo: /app/templates ou C:\Templates
        /// </summary>
        public string CaminhoRaiz { get; set; } = "/app/templates";

        /// <summary>
        /// Mapeamento de nomes amigáveis para arquivos físicos
        /// Exemplo: "RelatorioVistoria" -> "template_relatorio_vistoria.docx"
        /// </summary>
        public Dictionary<string, string> Templates { get; set; } = new();
    }
}
