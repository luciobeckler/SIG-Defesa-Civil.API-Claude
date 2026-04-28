using SIG_Defesa_Civil.API.Data.Models.SharePoint;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Services.SharePoint
{
    namespace SIG_Defesa_Civil.API
    {
        /// <summary>
        /// Serviço responsável pela integração com o SharePoint via Microsoft Graph API
        /// </summary>
        public interface ISharePointService
        {
            /// <summary>
            /// Faz upload de múltiplos arquivos para uma pasta estruturada no SharePoint.
            /// Estrutura esperada: /Ocorrencias/{ano}/{numeroProtocolo}/
            /// </summary>
            /// <param name="protocolo">Protocolo da ocorrência (ex: 2025-0042)</param>
            /// <param name="arquivos">Lista de arquivos a serem enviados</param>
            /// <returns>Lista com os IDs e URLs dos arquivos salvos no SharePoint</returns>
            /// <exception cref="SharePointUploadException">Lançada se qualquer upload falhar</exception>
            Task<List<SharePointUploadResult>> UploadArquivosAsync(
                string protocolo,
                List<(Stream FileStream, string FileName, TipoArquivo TipoArquivo)> arquivos);
        }
    }

}
