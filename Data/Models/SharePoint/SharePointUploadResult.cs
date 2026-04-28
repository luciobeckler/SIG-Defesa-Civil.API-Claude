namespace SIG_Defesa_Civil.API.Data.Models.SharePoint
{
    /// <summary>
    /// Retorno do upload de um arquivo no SharePoint
    /// </summary>
    public class SharePointUploadResult
    {
        public string ItemId { get; set; } = string.Empty;
        public string WebUrl { get; set; } = string.Empty;
        public string NomeArquivo { get; set; } = string.Empty;
    }
}
