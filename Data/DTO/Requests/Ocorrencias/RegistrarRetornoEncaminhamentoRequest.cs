using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Payload para registrar o retorno/conclusão de um encaminhamento.
    /// Pode ser enviado a qualquer momento após a ocorrência ser encerrada.
    /// Não altera o status da ocorrência.
    /// </summary>
    public class RegistrarRetornoEncaminhamentoRequest
    {
        /// <summary>Texto descritivo do retorno/conclusão do encaminhamento.</summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "O retorno do encaminhamento é obrigatório.")]
        [MaxLength(2000)]
        public string Retorno { get; set; } = string.Empty;
    }
}
