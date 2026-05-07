using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Exceptions;

namespace SIG_Defesa_Civil.API.Services.Storage
{
    /// <summary>
    /// Interface genérica para serviço de armazenamento de arquivos.
    /// Implementações podem usar file system local, rede compartilhada, ou nuvem.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Cria a estrutura de pastas para uma ocorrência no armazenamento.
        /// Estrutura gerada:
        ///   {BasePath}/{YYYY-XXXX}/Documentos/
        ///   {BasePath}/{YYYY-XXXX}/Fotos/Fotos_do_Municipe/
        ///   {BasePath}/{YYYY-XXXX}/Fotos/Fotos_da_Vistoria/
        /// </summary>
        /// <param name="protocolo">Protocolo da ocorrência (ex: 2026-0001)</param>
        /// <returns>Caminho absoluto da pasta raiz do protocolo</returns>
        /// <exception cref="StorageException">Se houver falha ao criar pastas (permissão, disco cheio, etc)</exception>
        Task<string> CriarEstruturaPastasAsync(string protocolo);

        /// <summary>
        /// Salva um arquivo no armazenamento e retorna o caminho relativo.
        /// </summary>
        /// <param name="protocolo">Protocolo da ocorrência</param>
        /// <param name="nomeArquivo">Nome do arquivo a ser salvo</param>
        /// <param name="tipoArquivo">Tipo/categoria do arquivo (FOTO_CIDADAO, COMPROVANTE_RESIDENCIA, etc)</param>
        /// <param name="stream">Stream com o conteúdo do arquivo</param>
        /// <returns>Caminho relativo do arquivo salvo (ex: /2026-0001/Documentos/foto_123.jpg)</returns>
        /// <exception cref="StorageException">Se houver falha ao gravar arquivo</exception>
        Task<string> SalvarArquivoAsync(
            string protocolo,
            string nomeArquivo,
            TipoArquivo tipoArquivo,
            Stream stream);

        /// <summary>
        /// Salva múltiplos arquivos de uma vez para uma ocorrência.
        /// </summary>
        /// <param name="protocolo">Protocolo da ocorrência</param>
        /// <param name="arquivos">Lista de tuplas (Stream, NomeArquivo, TipoArquivo)</param>
        /// <returns>Lista com os caminhos relativos dos arquivos salvos</returns>
        /// <exception cref="StorageException">Se qualquer arquivo falhar na gravação</exception>
        Task<List<string>> SalvarArquivosAsync(
            string protocolo,
            List<(Stream FileStream, string FileName, TipoArquivo TipoArquivo)> arquivos);

        /// <summary>
        /// Obtém o caminho absoluto de um arquivo a partir do caminho relativo.
        /// Útil para servir downloads ou validar existência.
        /// </summary>
        /// <param name="caminhoRelativo">Caminho relativo salvo no banco de dados</param>
        /// <returns>Caminho absoluto no file system</returns>
        string ObterCaminhoAbsoluto(string caminhoRelativo);

        /// <summary>
        /// Verifica se um arquivo existe no armazenamento.
        /// </summary>
        /// <param name="caminhoRelativo">Caminho relativo do arquivo</param>
        /// <returns>True se o arquivo existe, False caso contrário</returns>
        Task<bool> ArquivoExisteAsync(string caminhoRelativo);

        /// <summary>
        /// Lê um arquivo do armazenamento e retorna como stream.
        /// </summary>
        /// <param name="caminhoRelativo">Caminho relativo do arquivo</param>
        /// <returns>Stream do arquivo para leitura</returns>
        /// <exception cref="FileNotFoundException">Se o arquivo não existir</exception>
        /// <exception cref="StorageException">Se houver erro ao ler o arquivo</exception>
        Task<Stream> LerArquivoAsync(string caminhoRelativo);

    }
}
