namespace SIG_Defesa_Civil.API.Services.SharePoint
{
    using Azure.Identity;
    using global::SIG_Defesa_Civil.API.Data.Models.SharePoint;
    using global::SIG_Defesa_Civil.API.Data.Models.SharePoint.Configuration;
    using global::SIG_Defesa_Civil.API.Enums;
    using global::SIG_Defesa_Civil.API.Exceptions;
    using global::SIG_Defesa_Civil.API.Services.SharePoint.SIG_Defesa_Civil.API;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Graph.Models;
    using Microsoft.Graph.Models.ODataErrors;
    using System.Net;

    public class SharePointService : ISharePointService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly SharePointSettings _settings;
        private readonly ILogger<SharePointService> _logger;

        private string? _siteId;
        private string? _driveId; // No SDK v5, armazenar o Drive ID é obrigatório.

        public SharePointService(
            IOptions<SharePointSettings> settings,
            ILogger<SharePointService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            var clientSecretCredential = new ClientSecretCredential(
                _settings.TenantId,
                _settings.ClientId,
                _settings.ClientSecret
            );

            _graphClient = new GraphServiceClient(clientSecretCredential);
        }

        public async Task<List<SharePointUploadResult>> UploadArquivosAsync(
            string protocolo,
            List<(Stream FileStream, string FileName, TipoArquivo TipoArquivo)> arquivos)
        {
            try
            {
                _logger.LogInformation(
                    "Iniciando upload de {Count} arquivo(s) para o protocolo {Protocolo}",
                    arquivos.Count,
                    protocolo);

                // Garante a obtenção prévia de toda a cadeia de IDs necessária
                if (string.IsNullOrEmpty(_siteId) || string.IsNullOrEmpty(_driveId))
                {
                    await InitializeContextAsync();
                }

                var partes = protocolo.Split('-');
                if (partes.Length != 2)
                {
                    throw new SharePointUploadException($"Formato de protocolo inválido: {protocolo}");
                }

                var ano = partes[0];
                var numeroProtocolo = partes[1];

                var folderPath = $"Ocorrencias/{ano}/{numeroProtocolo}";
                await EnsureFolderStructureAsync(folderPath);

                var resultados = new List<SharePointUploadResult>();

                foreach (var (fileStream, fileName, tipoArquivo) in arquivos)
                {
                    var resultado = await UploadSingleFileAsync(folderPath, fileName, fileStream);
                    resultados.Add(resultado);

                    _logger.LogInformation(
                        "Arquivo {FileName} ({Tipo}) enviado com sucesso. ID: {ItemId}",
                        fileName,
                        tipoArquivo,
                        resultado.ItemId);
                }

                return resultados;
            }
            catch (ODataError ex)
            {
                _logger.LogError(ex,
                    "Erro da Graph API ao fazer upload para o protocolo {Protocolo}. Código: {Code}",
                    protocolo,
                    ex.ResponseStatusCode);

                throw new SharePointUploadException(
                    $"Falha na comunicação com o SharePoint: {ex.Error?.Message ?? ex.Message}",
                    ex);
            }
            catch (SharePointUploadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado ao fazer upload para o protocolo {Protocolo}",
                    protocolo);

                throw new SharePointUploadException(
                    "Erro inesperado durante o upload de arquivos",
                    ex);
            }
        }

        /// <summary>
        /// Obtém o Site ID e, crucialmente, o Drive ID base para navegação no SDK v5.
        /// </summary>
        private async Task InitializeContextAsync()
        {
            try
            {
                var uri = new Uri(_settings.SiteUrl);
                var hostname = uri.Host;
                var sitePath = uri.AbsolutePath;

                var site = await _graphClient.Sites[$"{hostname}:{sitePath}"].GetAsync();

                if (site?.Id == null)
                {
                    throw new SharePointUploadException("Não foi possível obter o ID do site SharePoint.");
                }
                _siteId = site.Id;

                // Consulta separada obrigatória no SDK v5+ para obter o ID do Drive do Site
                var drive = await _graphClient.Sites[_siteId].Drive.GetAsync();

                if (drive?.Id == null)
                {
                    throw new SharePointUploadException("Não foi possível obter o ID do Drive principal do site.");
                }
                _driveId = drive.Id;

                _logger.LogInformation("Contexto obtido. SiteId: {SiteId} | DriveId: {DriveId}", _siteId, _driveId);
            }
            catch (Exception ex)
            {
                throw new SharePointUploadException(
                    $"Erro ao resolver o contexto para a URL {_settings.SiteUrl}",
                    ex);
            }
        }

        private async Task EnsureFolderStructureAsync(string folderPath)
        {
            try
            {
                var folders = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var currentPath = string.Empty;

                foreach (var folder in folders)
                {
                    currentPath = string.IsNullOrEmpty(currentPath) ? folder : $"{currentPath}/{folder}";

                    try
                    {
                        await _graphClient.Drives[_driveId]
                            .Items[$"root:/{currentPath}"]
                            .GetAsync();
                    }
                    catch (ODataError ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        var parentPath = folders.Length == 1 ? "" : string.Join("/", folders.Take(folders.ToList().IndexOf(folder)));

                        var driveItem = new DriveItem
                        {
                            Name = folder,
                            Folder = new Folder(),
                            AdditionalData = new Dictionary<string, object>
                            {
                                { "@microsoft.graph.conflictBehavior", "rename" }
                            }
                        };

                        if (string.IsNullOrEmpty(parentPath))
                        {
                            await _graphClient.Drives[_driveId]
                                .Items["root"]
                                .Children
                                .PostAsync(driveItem);
                        }
                        else
                        {
                            await _graphClient.Drives[_driveId]
                                .Items[$"root:/{parentPath}:"]
                                .Children
                                .PostAsync(driveItem);
                        }

                        _logger.LogInformation("Pasta criada: {FolderPath}", currentPath);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SharePointUploadException(
                    $"Erro ao criar estrutura de pastas: {folderPath}",
                    ex);
            }
        }

        private async Task<SharePointUploadResult> UploadSingleFileAsync(
            string folderPath,
            string fileName,
            Stream fileStream)
        {
            try
            {
                var uploadPath = $"{folderPath}/{fileName}";

                var driveItem = await _graphClient.Drives[_driveId]
                    .Items[$"root:/{uploadPath}:"]
                    .Content
                    .PutAsync(fileStream);

                if (driveItem?.Id == null || driveItem.WebUrl == null)
                {
                    throw new SharePointUploadException($"Falha ao obter ID/URL do arquivo {fileName}");
                }

                return new SharePointUploadResult
                {
                    ItemId = driveItem.Id,
                    WebUrl = driveItem.WebUrl,
                    NomeArquivo = fileName
                };
            }
            catch (Exception ex)
            {
                throw new SharePointUploadException(
                    $"Erro ao fazer upload do arquivo {fileName}",
                    ex);
            }
        }
    }
}