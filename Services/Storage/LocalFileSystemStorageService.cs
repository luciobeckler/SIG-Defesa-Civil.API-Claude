using Microsoft.Extensions.Options;
using SIG_Defesa_Civil.API.Data.Configuration.Storage;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Exceptions;

namespace SIG_Defesa_Civil.API.Services.Storage
{
    public class LocalFileSystemStorageService : IStorageService
    {
        private readonly StorageSettings _settings;
        private readonly ILogger<LocalFileSystemStorageService> _logger;

        public LocalFileSystemStorageService(
            IOptions<StorageSettings> settings,
            ILogger<LocalFileSystemStorageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> CriarEstruturaPastasAsync(string protocolo)
        {
            // Cria as três folhas da árvore de armazenamento:
            //   [Protocolo]/Documentos/
            //   [Protocolo]/Fotos/Fotos_do_Municipe/
            //   [Protocolo]/Fotos/Fotos_da_Vistoria/
            var pastas = new[]
            {
                Path.Combine(_settings.BasePath, protocolo, _settings.SubPastaDocumentos),
                Path.Combine(_settings.BasePath, protocolo, _settings.SubPastaFotosMunicipe),
                Path.Combine(_settings.BasePath, protocolo, _settings.SubPastaFotosVistoria),
            };

            var pastaRaiz = Path.Combine(_settings.BasePath, protocolo);

            try
            {
                await Task.Run(() =>
                {
                    foreach (var pasta in pastas)
                        Directory.CreateDirectory(pasta);
                });

                _logger.LogInformation(
                    "Estrutura de pastas criada para protocolo {Protocolo}: {Pastas}",
                    protocolo, string.Join(", ", pastas));

                return pastaRaiz;
            }
            catch (UnauthorizedAccessException)
            {
                throw new StorageException(
                    $"Sem permissão para criar pastas em: {pastaRaiz}",
                    pastaRaiz,
                    StorageErrorType.PermissaoNegada);
            }
            catch (Exception ex) when (ex is PathTooLongException or DirectoryNotFoundException)
            {
                throw new StorageException(
                    $"Caminho inválido: {pastaRaiz}",
                    pastaRaiz,
                    StorageErrorType.CaminhoInvalido);
            }
            catch (IOException ex)
            {
                throw new StorageException(
                    $"Erro de I/O ao criar pastas: {pastaRaiz}",
                    ex,
                    StorageErrorType.ErroLeituraEscrita);
            }
        }

        /// <summary>
        /// Determina a subpasta de destino com base no tipo do arquivo:
        ///   COMPROVANTE_RESIDENCIA | FICHA_VISTORIA | RELATORIO_FINAL → Documentos
        ///   FOTO_CIDADAO                                               → Fotos/Fotos_do_Municipe
        ///   FOTO_CAMPO                                                 → Fotos/Fotos_da_Vistoria
        /// </summary>
        private string ObterSubPasta(TipoArquivo tipoArquivo) => tipoArquivo switch
        {
            TipoArquivo.FOTO_CIDADAO   => _settings.SubPastaFotosMunicipe,
            TipoArquivo.FOTO_CAMPO     => _settings.SubPastaFotosVistoria,
            _                          => _settings.SubPastaDocumentos,   // COMPROVANTE, FICHA, RELATORIO
        };

        public async Task<string> SalvarArquivoAsync(
            string protocolo,
            string nomeArquivo,
            TipoArquivo tipoArquivo,
            Stream stream)
        {
            var subPasta = ObterSubPasta(tipoArquivo);
            var caminhoRelativo = $"/{protocolo}/{subPasta}/{nomeArquivo}";
            var caminhoAbsoluto = ObterCaminhoAbsoluto(caminhoRelativo);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(caminhoAbsoluto)!);

                await using var fileStream = new FileStream(
                    caminhoAbsoluto,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await stream.CopyToAsync(fileStream);

                _logger.LogInformation(
                    "Arquivo salvo: {CaminhoRelativo} ({Tipo})",
                    caminhoRelativo,
                    tipoArquivo);

                return caminhoRelativo;
            }
            catch (UnauthorizedAccessException)
            {
                throw new StorageException(
                    $"Sem permissão para gravar arquivo: {caminhoAbsoluto}",
                    caminhoAbsoluto,
                    StorageErrorType.PermissaoNegada);
            }
            catch (IOException ex) when (IsDiskFull(ex))
            {
                throw new StorageException(
                    "Espaço em disco insuficiente para salvar o arquivo",
                    caminhoAbsoluto,
                    StorageErrorType.DiscoLotado);
            }
            catch (Exception ex) when (ex is PathTooLongException or DirectoryNotFoundException)
            {
                throw new StorageException(
                    $"Caminho inválido: {caminhoAbsoluto}",
                    caminhoAbsoluto,
                    StorageErrorType.CaminhoInvalido);
            }
            catch (IOException ex)
            {
                throw new StorageException(
                    $"Erro de I/O ao gravar arquivo: {caminhoAbsoluto}",
                    ex,
                    StorageErrorType.ErroLeituraEscrita);
            }
        }

        public async Task<List<string>> SalvarArquivosAsync(
            string protocolo,
            List<(Stream FileStream, string FileName, TipoArquivo TipoArquivo)> arquivos)
        {
            await CriarEstruturaPastasAsync(protocolo);

            var caminhos = new List<string>(arquivos.Count);

            foreach (var (stream, nomeArquivo, tipoArquivo) in arquivos)
            {
                var caminho = await SalvarArquivoAsync(protocolo, nomeArquivo, tipoArquivo, stream);
                caminhos.Add(caminho);
            }

            return caminhos;
        }

        public string ObterCaminhoAbsoluto(string caminhoRelativo)
        {
            return Path.Combine(_settings.BasePath, caminhoRelativo.TrimStart('/').TrimStart('\\'));
        }

        public Task<bool> ArquivoExisteAsync(string caminhoRelativo)
        {
            var caminhoAbsoluto = ObterCaminhoAbsoluto(caminhoRelativo);
            return Task.FromResult(File.Exists(caminhoAbsoluto));
        }

        public async Task<Stream> LerArquivoAsync(string caminhoRelativo)
        {
            var caminhoAbsoluto = ObterCaminhoAbsoluto(caminhoRelativo);

            if (!File.Exists(caminhoAbsoluto))
                throw new FileNotFoundException($"Arquivo não encontrado: {caminhoRelativo}", caminhoAbsoluto);

            try
            {
                var memoryStream = new MemoryStream();
                await using var fileStream = File.OpenRead(caminhoAbsoluto);
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (UnauthorizedAccessException)
            {
                throw new StorageException(
                    $"Sem permissão para ler arquivo: {caminhoAbsoluto}",
                    caminhoAbsoluto,
                    StorageErrorType.PermissaoNegada);
            }
            catch (IOException ex)
            {
                throw new StorageException(
                    $"Erro de I/O ao ler arquivo: {caminhoAbsoluto}",
                    ex,
                    StorageErrorType.ErroLeituraEscrita);
            }
        }

        // ERROR_DISK_FULL: Windows = 0x70 (112), Linux = ENOSPC (28)
        private static bool IsDiskFull(IOException ex) =>
            ex.HResult == unchecked((int)0x80070070) || ex.HResult == 28;
    }
}
