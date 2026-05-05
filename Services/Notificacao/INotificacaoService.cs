using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.Notificacao
{
    /// <summary>
    /// Gerencia os notificados da ocorrência — Etapa 5 do fluxo.
    /// Relacionamento 1:N: uma ocorrência pode ter múltiplos notificados.
    /// Ao registrar pelo menos um notificado, o status avança para NOTIFICADA.
    /// </summary>
    public interface INotificacaoService
    {
        /// <summary>
        /// Registra um ou mais notificados para a ocorrência.
        /// Pré-condição: ocorrência deve estar em VISTORIA_REALIZADA.
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
        /// A ocorrência retorna ao status VISTORIA_REALIZADA se não restar nenhum notificado.
        /// </summary>
        Task RemoverNotificadoAsync(int notificadoId, int usuarioId);
    }
}
