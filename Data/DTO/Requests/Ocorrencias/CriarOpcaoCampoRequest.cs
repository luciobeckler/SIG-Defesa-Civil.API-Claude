using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Cria uma opção personalizada para um campo de seleção da vistoria.
    /// </summary>
    public class CriarOpcaoCampoRequest
    {
        /// <summary>Chave do campo (ex.: AREA_AFETADA). Ver CamposVistoria.</summary>
        [Required] public string Campo { get; set; } = string.Empty;

        /// <summary>Texto da nova opção informado pelo usuário.</summary>
        [Required] public string Valor { get; set; } = string.Empty;
    }
}
