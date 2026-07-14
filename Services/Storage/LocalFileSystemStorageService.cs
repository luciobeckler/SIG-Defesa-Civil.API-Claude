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
            // Estrutura espelhando as categorias da Central de Documentos:
            //   [Protocolo]/Fotos_do_Cidadao/ … [Protocolo]/Relatorios_Assinados/ etc.
            var pastas = PastasArquivo.Padrao
                .Select(p => Path.Combine(_settings.BasePath, protocolo, p))
                .ToArray();

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
        /// Cada tipo de arquivo tem sua própria pasta, espelhando as categorias
        /// da Central de Documentos (ver <see cref="PastasArquivo"/>).
        /// </summary>
        private static string ObterSubPasta(TipoArquivo tipoArquivo) => PastasArquivo.De(tipoArquivo);

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

        public async Task<string> SalvarArquivoEmPastaAsync(
            string protocolo,
            string pasta,
            string nomeArquivo,
            Stream stream)
        {
            var pastaSegura = PastasArquivo.SanitizarNome(pasta);
            var caminhoRelativo = $"/{protocolo}/{pastaSegura}/{nomeArquivo}";
            var caminhoAbsoluto = ObterCaminhoAbsoluto(caminhoRelativo);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(caminhoAbsoluto)!);

                await using var fileStream = new FileStream(
                    caminhoAbsoluto, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 81920, useAsync: true);
                await stream.CopyToAsync(fileStream);

                _logger.LogInformation("Arquivo salvo em pasta personalizada: {Caminho}", caminhoRelativo);
                return caminhoRelativo;
            }
            catch (UnauthorizedAccessException)
            {
                throw new StorageException(
                    $"Sem permissão para gravar arquivo: {caminhoAbsoluto}",
                    caminhoAbsoluto, StorageErrorType.PermissaoNegada);
            }
            catch (IOException ex) when (IsDiskFull(ex))
            {
                throw new StorageException(
                    "Espaço em disco insuficiente para salvar o arquivo",
                    caminhoAbsoluto, StorageErrorType.DiscoLotado);
            }
            catch (IOException ex)
            {
                throw new StorageException(
                    $"Erro de I/O ao gravar arquivo: {caminhoAbsoluto}",
                    ex, StorageErrorType.ErroLeituraEscrita);
            }
        }

        public Task CriarPastaAsync(string protocolo, string pasta)
        {
            var pastaSegura = PastasArquivo.SanitizarNome(pasta);
            var caminho = Path.Combine(_settings.BasePath, protocolo, pastaSegura);
            try
            {
                Directory.CreateDirectory(caminho);
                _logger.LogInformation("Pasta criada para {Protocolo}: {Pasta}", protocolo, pastaSegura);
                return Task.CompletedTask;
            }
            catch (UnauthorizedAccessException)
            {
                throw new StorageException(
                    $"Sem permissão para criar pasta: {caminho}",
                    caminho, StorageErrorType.PermissaoNegada);
            }
        }

        public Task<List<string>> ListarPastasAsync(string protocolo)
        {
            var raiz = Path.Combine(_settings.BasePath, protocolo);
            if (!Directory.Exists(raiz))
                return Task.FromResult(new List<string>());

            var pastas = Directory.GetDirectories(raiz)
                .Select(d => Path.GetFileName(d)!)
                .OrderBy(p => p)
                .ToList();
            return Task.FromResult(pastas);
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
