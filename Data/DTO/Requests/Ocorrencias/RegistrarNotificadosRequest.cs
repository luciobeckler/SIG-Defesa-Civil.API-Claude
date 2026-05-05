using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Registro dos notificados da ocorrência — Etapa 5.
    /// Ao registrar pelo menos um notificado, o status avança para NOTIFICADA.
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
    }
}
