using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos
{
    public class ArquivoUploadDto
    {
        /// <summary>
        /// Tipo do arquivo: FOTO_CIDADAO, COMPROVANTE_RESIDENCIA, FOTO_CAMPO, etc.
        /// </summary>
        public TipoArquivo TipoArquivo { get; set; }

        /// <summary>
        /// Arquivo binário enviado via multipart/form-data
        /// </summary>
        public IFormFile File { get; set; } = null!;
    }
}
