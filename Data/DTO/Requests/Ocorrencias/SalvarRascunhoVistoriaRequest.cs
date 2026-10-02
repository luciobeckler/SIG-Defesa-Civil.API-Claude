using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>Preenchimento em andamento enviado pelo aplicativo.</summary>
    public class SalvarRascunhoVistoriaRequest
    {
        /// <summary>
        /// Valores do formulário em JSON. Chega como texto porque o rascunho
        /// acompanha o formulário do aplicativo, sem contrato fixo na API.
        /// </summary>
        [Required]
        public string ConteudoJson { get; set; } = "{}";
    }
}
