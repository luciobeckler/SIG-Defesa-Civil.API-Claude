using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.AvaliacaoRisco
{
    /// <summary>
    /// Gerencia a avaliação inicial de risco — Etapa 2 do fluxo.
    /// Ao registrar uma avaliação, o status da ocorrência avança para EM_AVALIACAO.
    /// </summary>
    public interface IAvaliacaoRiscoService
    {
        /// <summary>
        /// Registra a avaliação inicial de risco para uma ocorrência.
        /// Pré-condição: ocorrência deve estar com status ABERTA.
        /// </summary>
        Task<AvaliacaoRiscoDto> RegistrarAsync(
            int ocorrenciaId,
            RegistrarAvaliacaoRiscoRequest request,
            int usuarioId);

        /// <summary>
        /// Retorna a avaliação de risco de uma ocorrência.
        /// Retorna null se a Etapa 2 ainda não foi preenchida.
        /// </summary>
        Task<AvaliacaoRiscoDto?> ObterPorOcorrenciaAsync(int ocorrenciaId);

        /// <summary>
        /// Atualiza a avaliação de risco existente.
        /// Pré-condição: avaliação deve existir para a ocorrência.
        /// </summary>
        Task<AvaliacaoRiscoDto> AtualizarAsync(
            int ocorrenciaId,
            RegistrarAvaliacaoRiscoRequest request,
            int usuarioId);
    }
}
