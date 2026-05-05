using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Exceptions
{
    /// <summary>
    /// Exceção lançada quando ocorre falha no armazenamento de arquivos.
    /// Pode ser causada por: falta de permissão, disco cheio, caminho inválido, I/O error.
    /// Esta exceção dispara o rollback da transação do banco de dados.
    /// </summary>
    public class StorageException : Exception
    {
        public string? CaminhoArquivo { get; set; }
        public StorageErrorType TipoErro { get; set; }

        public StorageException(string message, StorageErrorType tipoErro = StorageErrorType.Generico)
            : base(message)
        {
            TipoErro = tipoErro;
        }

        public StorageException(
            string message,
            Exception innerException,
            StorageErrorType tipoErro = StorageErrorType.Generico)
            : base(message, innerException)
        {
            TipoErro = tipoErro;
        }

        public StorageException(
            string message,
            string caminhoArquivo,
            StorageErrorType tipoErro = StorageErrorType.Generico)
            : base(message)
        {
            CaminhoArquivo = caminhoArquivo;
            TipoErro = tipoErro;
        }
    }
}
