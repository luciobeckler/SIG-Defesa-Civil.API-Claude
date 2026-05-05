using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.Vistoria
{
    /// <summary>
    /// Gerencia o agendamento (Etapa 3) e a execução da vistoria presencial (Etapa 4).
    /// As duas etapas são agrupadas neste serviço por coesão — a vistoria depende do agendamento.
    /// </summary>
    public interface IVistoriaService
    {
        // ── Etapa 3: Agendamento ──────────────────────────────────────────────────

        /// <summary>
        /// Designa a equipe de vistoriadores e registra a primeira tentativa.
        /// Pré-condição: ocorrência deve estar em EM_AVALIACAO.
        /// Status avança para VISTORIA_SOLICITADA.
        /// </summary>
        Task<AgendamentoVistoriaDto> AgendarAsync(
            int ocorrenciaId,
            RegistrarAgendamentoVistoriaRequest request,
            int usuarioId);

        /// <summary>
        /// Retorna o agendamento da ocorrência.
        /// Retorna null se a Etapa 3 ainda não foi preenchida.
        /// </summary>
        Task<AgendamentoVistoriaDto?> ObterAgendamentoPorOcorrenciaAsync(int ocorrenciaId);

        /// <summary>
        /// Adiciona uma nova tentativa de comparecimento a um agendamento existente.
        /// Máximo de 3 tentativas por agendamento.
        /// </summary>
        Task<AgendamentoVistoriaDto> AdicionarTentativaAsync(
            int agendamentoId,
            AdicionarTentativaRequest request,
            int usuarioId);

        // ── Etapa 4: Vistoria Presencial ──────────────────────────────────────────

        /// <summary>
        /// Registra o resultado da vistoria presencial de campo.
        /// Pré-condição: ocorrência deve estar em VISTORIA_SOLICITADA.
        /// Status avança para VISTORIA_REALIZADA.
        /// </summary>
        Task<VistoriaDto> RegistrarVistoriaAsync(
            int ocorrenciaId,
            RegistrarVistoriaRequest request,
            int usuarioId);

        /// <summary>
        /// Retorna a vistoria presencial de uma ocorrência.
        /// Retorna null se a Etapa 4 ainda não foi preenchida.
        /// </summary>
        Task<VistoriaDto?> ObterVistoriaPorOcorrenciaAsync(int ocorrenciaId);

        /// <summary>
        /// Atualiza os dados da vistoria presencial registrada.
        /// </summary>
        Task<VistoriaDto> AtualizarVistoriaAsync(
            int ocorrenciaId,
            RegistrarVistoriaRequest request,
            int usuarioId);
    }
}
