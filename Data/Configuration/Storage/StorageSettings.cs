namespace SIG_Defesa_Civil.API.Data.Configuration.Storage
{
    public class StorageSettings
    {
        public string BasePath { get; set; } = string.Empty;
        public string SubPastaDocumentos { get; set; } = "Documentos";
        public long TamanhoMaximoArquivo { get; set; } = 10_485_760;
        public bool ValidarPermissoesInicializacao { get; set; } = true;
    }
}
