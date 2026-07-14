using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>Cria uma pasta personalizada na Central de Documentos (ex.: "Retorno").</summary>
    public class CriarPastaRequest
    {
        [Required, MaxLength(50)]
        public string Nome { get; set; } = string.Empty;
    }
}
