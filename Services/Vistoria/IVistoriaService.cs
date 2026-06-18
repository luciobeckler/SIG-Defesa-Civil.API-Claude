using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.Vistoria
{
    /// <summary>
    /// Gerencia o agendamento (Etapa 3) e a execução da vistoria presencial (Etapa 4).
    /// Ambas as etapas suportam multiplicidade: uma ocorrência pode ter N agendamentos e N vistorias.
    /// </summary>
    public interface IVistoriaService
    {
        // ── Etapa 3: Agendamento ──────────────────────────────────────────────────

        /// <summary>
        /// Designa a equipe de vistoriadores e registra a primeira tentativa.
        /// Pré-condição: ocorrência deve estar em EM_AVALIACAO (primeiro agendamento)
        /// ou em VISTORIA_SOLICITADA (re-agendamento).
        /// Status avança para VISTORIA_SOLICITADA se ainda não estiver.
        /// </summary>
        Task<AgendamentoVistoriaDto> AgendarAsync(
            int ocorrenciaId,
            RegistrarAgendamentoVistoriaRequest request,
            int usuarioId);

        /// <summary>
        /// Retorna todos os agendamentos da ocorrência em ordem crescente de Numero.
        /// </summary>
        Task<List<AgendamentoVistoriaDto>> ListarAgendamentosAsync(int ocorrenciaId);

        /// <summary>
        /// Retorna um agendamento específico pelo seu ID.
        /// </summary>
        Task<AgendamentoVistoriaDto?> ObterAgendamentoPorIdAsync(int agendamentoId);

        /// <summary>
        /// Adiciona uma nova tentativa de comparecimento a um agendamento existente.
        /// Máximo de 3 tentativas por agendamento.
        /// </summary>
        Task<AgendamentoVistoriaDto> AdicionarTentativaAsync(
            int agendamentoId,
            AdicionarTentativaRequest request,
            int usuarioId);

        /// <summary>
        /// Atribui a equipe de vistoriadores a um agendamento — passo posterior ao
        /// agendamento. Os vistoriadores designados poderão baixar a ocorrência para
        /// uso offline. Pré-condição: agendamento deve pertencer à ocorrência e estar ATIVO.
        /// </summary>
        Task<AgendamentoVistoriaDto> AtribuirVistoriadoresAsync(
            int ocorrenciaId,
            int agendamentoId,
            AtribuirVistoriadoresRequest request,
            int usuarioId);

        // ── Etapa 4: Vistoria Presencial ──────────────────────────────────────────

        /// <summary>
        /// Registra o resultado da vistoria presencial de campo.
        /// Pré-condição: ocorrência deve estar em VISTORIA_SOLICITADA e o agendamento
        /// ATIVO já deve ter os vistoriadores atribuídos (a equipe é derivada dele).
        /// Status avança para VISTORIA_REALIZADA e o agendamento é marcado como CONCLUIDO.
        /// </summary>
        Task<VistoriaDto> RegistrarVistoriaAsync(
            int ocorrenciaId,
            RegistrarVistoriaRequest request,
            int usuarioId);

        /// <summary>
        /// Retorna todas as vistorias da ocorrência em ordem crescente de Numero.
        /// </summary>
        Task<List<VistoriaDto>> ListarVistoriasAsync(int ocorrenciaId);

        /// <summary>
        /// Retorna uma vistoria específica pelo seu ID.
        /// </summary>
        Task<VistoriaDto?> ObterVistoriaPorIdAsync(int vistoriaId);

        /// <summary>
        /// Atualiza os dados de uma vistoria presencial já registrada (por ID).
        /// </summary>
        Task<VistoriaDto> AtualizarVistoriaPorIdAsync(
            int vistoriaId,
            RegistrarVistoriaRequest request,
            int usuarioId);

        /// <summary>
        /// Remove um agendamento e suas tentativas de comparecimento.
        /// Lança <see cref="InvalidOperationException"/> se o agendamento não pertencer à ocorrência.
        /// </summary>
        Task ExcluirAgendamentoAsync(int ocorrenciaId, int agendamentoId, int usuarioId);

        /// <summary>
        /// Adiciona fotos de campo (FOTO_CAMPO) a uma vistoria já registrada.
        /// Os arquivos são salvos em [Protocolo]/Fotos/Fotos_da_Vistoria/.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência dona da vistoria</param>
        /// <param name="vistoriaId">ID da vistoria que receberá as fotos</param>
        /// <param name="fotos">Arquivos de foto de campo</param>
        /// <param name="usuarioId">ID do usuário que está fazendo o upload</param>
        /// <returns>Número de fotos salvas</returns>
        Task<int> AdicionarFotosCampoAsync(
            int ocorrenciaId,
            int vistoriaId,
            List<IFormFile> fotos,
            int usuarioId);
    }
}
