using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.Notificacao
{
    /// <summary>
    /// Gerencia os notificados da ocorrência — quem recebeu o relatório.
    /// Propriedade da ocorrência (não é etapa): pode ser registrada a qualquer
    /// momento e não altera o status do fluxo.
    /// </summary>
    public interface INotificacaoService
    {
        /// <summary>
        /// Registra um ou mais notificados (recebedores do relatório).
        /// Permitido em qualquer status, exceto CANCELADA.
        /// </summary>
        Task<List<NotificadoDto>> RegistrarAsync(
            int ocorrenciaId,
            RegistrarNotificadosRequest request,
            int usuarioId);

        /// <summary>
        /// Lista todos os notificados de uma ocorrência em ordem cronológica.
        /// </summary>
        Task<List<NotificadoDto>> ListarPorOcorrenciaAsync(int ocorrenciaId);

        /// <summary>
        /// Remove um notificado específico (deleção física — notificados não têm soft-delete próprio).
        /// </summary>
        Task RemoverNotificadoAsync(int notificadoId, int usuarioId);

        /// <summary>
        /// Salva a assinatura do notificado (PNG do canvas) — obrigatória quando o
        /// recebimento do relatório é PRESENCIAL. Substitui assinatura anterior.
        /// </summary>
        Task SalvarAssinaturaNotificadoAsync(
            int ocorrenciaId, int notificadoId, IFormFile arquivo, int usuarioId);
    }
}
