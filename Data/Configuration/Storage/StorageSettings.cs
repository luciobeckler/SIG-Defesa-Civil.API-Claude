namespace SIG_Defesa_Civil.API.Data.Configuration.Storage
{
    public class StorageSettings
    {
        public string BasePath { get; set; } = string.Empty;

        // ── Subpastas da árvore de armazenamento ──────────────────────────────
        /// <summary>Documentos: comprovantes, fichas e relatórios.</summary>
        public string SubPastaDocumentos { get; set; } = "Documentos";

        /// <summary>Fotos enviadas pelo cidadão na abertura da ocorrência.</summary>
        public string SubPastaFotosMunicipe { get; set; } = "Fotos/Fotos_do_Municipe";

        /// <summary>Fotos tiradas em campo pelos vistoriadores.</summary>
        public string SubPastaFotosVistoria { get; set; } = "Fotos/Fotos_da_Vistoria";

        public long TamanhoMaximoArquivo { get; set; } = 10_485_760;
        public bool ValidarPermissoesInicializacao { get; set; } = true;
    }
}
