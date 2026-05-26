using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SIG_Defesa_Civil.API.Data.Configuration.Storage;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Exceptions;

namespace SIG_Defesa_Civil.API.Services.Storage
{
    /// <summary>
    /// Implementação de <see cref="IStorageService"/> usando Cloudflare R2 (S3-compatível).
    /// Ativada automaticamente em produção quando as variáveis de ambiente do R2 estiverem presentes.
    /// O caminho relativo salvo no banco de dados é idêntico ao da implementação local,
    /// garantindo portabilidade entre os dois providers.
    /// </summary>
    public class R2StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly R2StorageSettings _settings;
        private readonly ILogger<R2StorageService> _logger;

        public R2StorageService(
            IAmazonS3 s3,
            IOptions<R2StorageSettings> settings,
            ILogger<R2StorageService> logger)
        {
            _s3 = s3;
            _settings = settings.Value;
            _logger = logger;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// No R2/S3 não existem pastas reais — a hierarquia é simulada por prefixos de chave.
        /// Retorna o prefixo raiz do protocolo; nenhuma chamada à API é necessária.
        /// </summary>
        public Task<string> CriarEstruturaPastasAsync(string protocolo)
        {
            _logger.LogDebug("R2: estrutura de prefixos implícita para protocolo {Protocolo}", protocolo);
            return Task.FromResult(protocolo);
        }

        /// <summary>Mapeia o tipo de arquivo para a subpasta correspondente.</summary>
        private string ObterSubPasta(TipoArquivo tipoArquivo) => tipoArquivo switch
        {
            TipoArquivo.FOTO_CIDADAO => _settings.SubPastaFotosMunicipe,
            TipoArquivo.FOTO_CAMPO   => _settings.SubPastaFotosVistoria,
            _                        => _settings.SubPastaDocumentos,
        };

        /// <summary>
        /// Converte o caminho relativo gravado no banco (ex: /2026-0001/Documentos/foto.jpg)
        /// para chave S3 sem a barra inicial (ex: 2026-0001/Documentos/foto.jpg).
        /// </summary>
        private static string ToS3Key(string caminhoRelativo) =>
            caminhoRelativo.TrimStart('/').TrimStart('\\');

        // ── IStorageService ───────────────────────────────────────────────────────

        public async Task<string> SalvarArquivoAsync(
            string protocolo,
            string nomeArquivo,
            TipoArquivo tipoArquivo,
            Stream stream)
        {
            var subPasta = ObterSubPasta(tipoArquivo);
            var caminhoRelativo = $"/{protocolo}/{subPasta}/{nomeArquivo}";
            var s3Key = ToS3Key(caminhoRelativo);

            try
            {
                var request = new PutObjectRequest
                {
                    BucketName          = _settings.BucketName,
                    Key                 = s3Key,
                    InputStream         = stream,
                    AutoCloseStream     = false,
                    // R2 não suporta STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER.
                    // DisablePayloadSigning força o modo UNSIGNED-PAYLOAD, compatível com R2.
                    DisablePayloadSigning = true,
                };

                await _s3.PutObjectAsync(request);

                _logger.LogInformation(
                    "Arquivo salvo no R2: {Key} ({Tipo})", s3Key, tipoArquivo);

                return caminhoRelativo;
            }
            catch (AmazonS3Exception ex)
            {
                throw new StorageException(
                    $"Erro ao salvar arquivo no R2: {s3Key}",
                    ex,
                    StorageErrorType.ErroLeituraEscrita);
            }
        }

        public async Task<List<string>> SalvarArquivosAsync(
            string protocolo,
            List<(Stream FileStream, string FileName, TipoArquivo TipoArquivo)> arquivos)
        {
            var caminhos = new List<string>(arquivos.Count);
            foreach (var (stream, nomeArquivo, tipoArquivo) in arquivos)
            {
                var caminho = await SalvarArquivoAsync(protocolo, nomeArquivo, tipoArquivo, stream);
                caminhos.Add(caminho);
            }
            return caminhos;
        }

        /// <summary>
        /// Retorna a chave S3 equivalente ao caminho relativo.
        /// Mantida por compatibilidade com código que chame ObterCaminhoAbsoluto diretamente.
        /// </summary>
        public string ObterCaminhoAbsoluto(string caminhoRelativo) =>
            ToS3Key(caminhoRelativo);

        public async Task<bool> ArquivoExisteAsync(string caminhoRelativo)
        {
            try
            {
                await _s3.GetObjectMetadataAsync(_settings.BucketName, ToS3Key(caminhoRelativo));
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<Stream> LerArquivoAsync(string caminhoRelativo)
        {
            var s3Key = ToS3Key(caminhoRelativo);

            try
            {
                var response = await _s3.GetObjectAsync(_settings.BucketName, s3Key);

                // Copia para MemoryStream para encapsular o ciclo de vida da conexão HTTP
                var ms = new MemoryStream();
                await response.ResponseStream.CopyToAsync(ms);
                ms.Position = 0;
                return ms;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException(
                    $"Arquivo não encontrado no R2: {caminhoRelativo}", s3Key);
            }
            catch (AmazonS3Exception ex)
            {
                throw new StorageException(
                    $"Erro ao ler arquivo do R2: {s3Key}",
                    ex,
                    StorageErrorType.ErroLeituraEscrita);
            }
        }
    }
}
