namespace SIG_Defesa_Civil.API.Exceptions
{
    /// <summary>
    /// Exceção lançada quando ocorre falha no upload para o SharePoint.
    /// Esta exceção dispara o rollback da transação do banco de dados.
    /// </summary>
    public class SharePointUploadException : Exception
    {
        public SharePointUploadException(string message) : base(message)
        {
        }

        public SharePointUploadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
