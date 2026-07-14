using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Registro de quem recebeu o relatório da ocorrência (notificados).
    /// Propriedade da ocorrência — pode ser registrado a qualquer momento,
    /// sem alterar o status do fluxo.
    /// </summary>
    public class RegistrarNotificadosRequest
    {
        /// <summary>Lista de notificados. Deve conter ao menos um item.</summary>
        [Required]
        [MinLength(1, ErrorMessage = "Informe pelo menos um notificado.")]
        public List<NotificadoItemRequest> Notificados { get; set; } = new();
    }

    /// <summary>Dados de um único notificado.</summary>
    public class NotificadoItemRequest
    {
        [Required] public string Nome { get; set; } = string.Empty;

        /// <summary>RG ou CPF do notificado (campo unificado conforme planilha).</summary>
        public string? RgCpf { get; set; }

        [Required] public DateOnly DataNotificacao { get; set; }

        /// <summary>
        /// Como o relatório foi recebido (EMAIL ou PRESENCIAL).
        /// PRESENCIAL exige a coleta da assinatura do notificado em seguida.
        /// </summary>
        public FormaRecebimentoRelatorio FormaRecebimento { get; set; } = FormaRecebimentoRelatorio.EMAIL;
    }
}
