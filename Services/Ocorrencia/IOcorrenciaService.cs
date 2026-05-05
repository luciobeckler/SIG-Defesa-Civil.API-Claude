using SIG_Defesa_Civil.API.Data.DTO.Requests;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.Ocorrencia
{
    /// <summary>
    /// Gerencia o ciclo de vida da ocorrência: CRUD da Etapa 1, paginação, LGPD e soft-delete.
    /// Operações das Etapas 2–6 ficam nos serviços específicos de cada etapa.
    /// </summary>
    public interface IOcorrenciaService
    {
        // ── Etapa 1: Abertura ─────────────────────────────────────────────────────

        /// <summary>
        /// Cria uma nova ocorrência (Etapa 1) a partir de uma solicitação pública de cidadão.
        /// Transação "All-or-Nothing": banco + storage de arquivos.
        /// <para>
        /// O <c>CriadoPorId</c> é preenchido automaticamente com o ID do solicitante
        /// obtido/criado a partir do CPF — cidadãos não possuem conta prévia no sistema.
        /// </para>
        /// </summary>
        Task<OcorrenciaCriadaDto> CriarOcorrenciaAsync(CriarOcorrenciaRequest request);

        /// <summary>
        /// Retorna o detalhe completo de uma ocorrência (todas as etapas preenchidas).
        /// Dados do solicitante são mascarados por padrão (LGPD).
        /// </summary>
        Task<OcorrenciaDetalheDto> ObterDetalhesAsync(int ocorrenciaId);

        /// <summary>
        /// Atualiza dados da Etapa 1 (solicitante, local, descrição).
        /// Apenas campos enviados são atualizados (PATCH semântico).
        /// </summary>
        Task<OcorrenciaCriadaDto> AtualizarOcorrenciaAsync(
            int ocorrenciaId,
            AtualizarOcorrenciaRequest request,
            int usuarioId);

        /// <summary>
        /// Soft-delete: preenche DeletedAt e ExcluidoPorId.
        /// O registro continua no banco para fins de auditoria.
        /// </summary>
        Task ExcluirAsync(int ocorrenciaId, int usuarioId, string? motivo = null);

        /// <summary>
        /// Restaura uma ocorrência previamente excluída (limpa DeletedAt).
        /// </summary>
        Task RestaurarAsync(int ocorrenciaId, int usuarioId);

        // ── Listagem e LGPD ───────────────────────────────────────────────────────

        /// <summary>
        /// Lista ocorrências com dados sensíveis mascarados (LGPD).
        /// Registros com DeletedAt preenchido são excluídos da listagem padrão.
        /// </summary>
        Task<List<OcorrenciaListagemDto>> ListarOcorrenciasMascaradasAsync(
            FiltroOcorrenciaDto? filtros = null,
            PaginacaoDto? paginacao = null);

        /// <summary>
        /// Revela dados sensíveis de uma ocorrência.
        /// CRÍTICO: grava log obrigatório em log_acesso_lgpd dentro de transação.
        /// </summary>
        Task<OcorrenciaDadosSensiveisDto> RevelarDadosSensiveisAsync(
            int ocorrenciaId,
            RevelarDadosRequest request,
            string ipOrigem);

        // ── Documentos ────────────────────────────────────────────────────────────

        /// <summary>
        /// Gera documentos Word em lote via templates pré-definidos.
        /// Continua o processamento mesmo se uma geração individual falhar.
        /// </summary>
        Task<GeracaoLoteResultadoDto> GerarDocumentosEmLoteAsync(GerarDocumentosLoteRequest request);
    }
}
