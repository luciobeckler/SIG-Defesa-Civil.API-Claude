namespace SIG_Defesa_Civil.API.Data.Models.SharePoint.Configuration
{
    /// <summary>
    /// Configurações para autenticação e acesso ao SharePoint via Microsoft Graph API
    /// </summary>
    public class SharePointSettings
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// URL do site SharePoint (ex: https://contoso.sharepoint.com/sites/DefesaCivil)
        /// </summary>
        public string SiteUrl { get; set; } = string.Empty;

        /// <summary>
        /// Nome da biblioteca de documentos onde os arquivos serão salvos
        /// </summary>
        public string DocumentLibrary { get; set; } = "Documentos";
    }
}
