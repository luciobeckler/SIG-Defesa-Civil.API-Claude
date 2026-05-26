using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.Encaminhamento
{
    /// <summary>
    /// Gerencia os encaminhamentos formais e encerramento da ocorrência — Etapa 6 do fluxo.
    /// Ao registrar o encaminhamento, o status avança para ENCERRADA.
    /// </summary>
    public interface IEncaminhamentoService
    {
        /// <summary>
        /// Registra os encaminhamentos formais e encerra a ocorrência.
        /// Pré-condição: ocorrência deve estar em NOTIFICADA.
        /// Status avança para ENCERRADA.
        /// </summary>
        Task<EncaminhamentoFinalDto> RegistrarAsync(
            int ocorrenciaId,
            RegistrarEncaminhamentoRequest request,
            int usuarioId);

        /// <summary>
        /// Retorna o encaminhamento final de uma ocorrência.
        /// Retorna null se a Etapa 6 ainda não foi preenchida.
        /// </summary>
        Task<EncaminhamentoFinalDto?> ObterPorOcorrenciaAsync(int ocorrenciaId);

        /// <summary>
        /// Atualiza o encaminhamento final (ex: adição do arquivo de relatório após upload).
        /// </summary>
        Task<EncaminhamentoFinalDto> AtualizarAsync(
            int ocorrenciaId,
            RegistrarEncaminhamentoRequest request,
            int usuarioId);

        /// <summary>
        /// Registra ou atualiza o retorno/conclusão de um encaminhamento.
        /// Pode ser chamado a qualquer momento após o encaminhamento final existir,
        /// inclusive com a ocorrência já ENCERRADA. Não altera o status.
        /// </summary>
        Task<EncaminhamentoFinalDto> RegistrarRetornoAsync(int ocorrenciaId, string retorno, int usuarioId);

        /// <summary>
        /// Reabre uma ocorrência encerrada — retorna o status para NOTIFICADA.
        /// Use quando for necessário corrigir o encaminhamento.
        /// </summary>
        Task ReabrirAsync(int ocorrenciaId, int usuarioId, string motivo);
    }
}
