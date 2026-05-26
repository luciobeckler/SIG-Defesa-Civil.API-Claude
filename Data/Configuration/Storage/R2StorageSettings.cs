namespace SIG_Defesa_Civil.API.Data.Configuration.Storage
{
    /// <summary>
    /// Configurações do Cloudflare R2 (S3-compatível).
    /// Em produção, preencher via variáveis de ambiente no Render:
    ///   R2Storage__AccountId, R2Storage__AccessKeyId,
    ///   R2Storage__SecretAccessKey, R2Storage__BucketName
    /// </summary>
    public class R2StorageSettings
    {
        /// <summary>Account ID do Cloudflare (disponível no painel R2).</summary>
        public string AccountId { get; set; } = string.Empty;

        /// <summary>Access Key ID gerada nas credenciais de API do R2.</summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>Secret Access Key gerada nas credenciais de API do R2.</summary>
        public string SecretAccessKey { get; set; } = string.Empty;

        /// <summary>Nome do bucket R2 criado para o projeto.</summary>
        public string BucketName { get; set; } = string.Empty;

        // ── Subpastas (mesma estrutura do LocalFileSystemStorageService) ─────────
        public string SubPastaDocumentos { get; set; } = "Documentos";
        public string SubPastaFotosMunicipe { get; set; } = "Fotos/Fotos_do_Municipe";
        public string SubPastaFotosVistoria { get; set; } = "Fotos/Fotos_da_Vistoria";
        public long TamanhoMaximoArquivo { get; set; } = 10_485_760;

        // ── Derivados ─────────────────────────────────────────────────────────────
        /// <summary>Endpoint S3-compatível do R2 para esta conta.</summary>
        public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";

        /// <summary>True quando as credenciais mínimas estão preenchidas.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccountId) &&
            !string.IsNullOrWhiteSpace(AccessKeyId) &&
            !string.IsNullOrWhiteSpace(SecretAccessKey) &&
            !string.IsNullOrWhiteSpace(BucketName);
    }
}
