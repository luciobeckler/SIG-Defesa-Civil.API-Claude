using Microsoft.AspNetCore.Http;
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
        /// <summary>
        /// Contagem de ocorrências por status, respeitando os mesmos filtros da
        /// listagem. Usado pelas abas (Ativas/Histórico) e pelo kanban.
        /// </summary>
        Task<ResumoOcorrenciasDto> ObterResumoAsync(FiltroOcorrenciaDto? filtros = null);

        Task<OcorrenciaDadosSensiveisDto> RevelarDadosSensiveisAsync(
            int ocorrenciaId,
            RevelarDadosRequest request,
            string ipOrigem);

        // ── Documentos ────────────────────────────────────────────────────────────

        /// <summary>
        /// Lista os arquivos de uma ocorrência para a Central de Documentos.
        /// Aceita filtro opcional por categoria (string enum TipoArquivo).
        /// Lança <see cref="InvalidOperationException"/> se a ocorrência não existir.
        /// </summary>
        Task<List<ArquivoListagemDto>> ListarArquivosAsync(int ocorrenciaId, string? tipoArquivo = null);

        /// <summary>
        /// Gera documentos Word em lote via templates pré-definidos.
        /// Continua o processamento mesmo se uma geração individual falhar.
        /// </summary>
        Task<GeracaoLoteResultadoDto> GerarDocumentosEmLoteAsync(GerarDocumentosLoteRequest request);

        /// <summary>
        /// Consulta pública para o cidadão acompanhar sua ocorrência via protocolo + CPF.
        /// Retorna dados mascarados (LGPD). Não requer autenticação.
        /// Lança <see cref="UnauthorizedAccessException"/> se o CPF não corresponder ao solicitante.
        /// Lança <see cref="InvalidOperationException"/> se o protocolo não existir.
        /// </summary>
        Task<OcorrenciaDetalheDto> AcompanharAsync(string protocolo, string cpf);

        // ── Assinatura do Munícipe ────────────────────────────────────────────────

        /// <summary>
        /// Salva a assinatura digital do munícipe (PNG do canvas) vinculada a uma vistoria específica.
        /// Tipo de arquivo: ASSINATURA_MUNICIPIO. Substitui assinatura anterior da mesma vistoria se existir.
        /// Lança <see cref="InvalidOperationException"/> se a vistoria não pertencer à ocorrência.
        /// </summary>
        Task SalvarAssinaturaAsync(int ocorrenciaId, int vistoriaId, IFormFile arquivo, int usuarioId);

        // ── Central de Documentos ────────────────────────────────────────────────

        /// <summary>
        /// Lista as pastas da ocorrência na Central de Documentos:
        /// padrão (uma por tipo de arquivo) + personalizadas criadas pelo usuário.
        /// </summary>
        Task<List<string>> ListarPastasAsync(int ocorrenciaId);

        /// <summary>
        /// Cria uma pasta personalizada para a ocorrência (ex.: "Retorno").
        /// Idempotente. Retorna a lista atualizada de pastas.
        /// </summary>
        Task<List<string>> CriarPastaAsync(int ocorrenciaId, string nome, int usuarioId);

        /// <summary>
        /// Adiciona arquivos a uma pasta da Central de Documentos.
        /// Pastas padrão gravam com o TipoArquivo correspondente; pastas
        /// personalizadas gravam com o nome da pasta como categoria.
        /// </summary>
        Task<int> AdicionarArquivosAsync(int ocorrenciaId, string pasta, List<IFormFile> arquivos, int usuarioId);

        // ── Retorno do relatório assinado / acompanhamento ───────────────────────

        /// <summary>
        /// Salva o relatório final preenchido e assinado (PDF) — etapa de retorno
        /// do relatório. Substitui o PDF anterior se já existir.
        /// </summary>
        Task SalvarRelatorioAssinadoAsync(int ocorrenciaId, IFormFile arquivo, int usuarioId);

        /// <summary>
        /// Retorna o relatório final para o acompanhamento do cidadão (protocolo + CPF).
        /// Prefere o PDF assinado; retorna null se ainda não houver relatório disponível.
        /// </summary>
        Task<(Stream Conteudo, string Nome, string ContentType)?> ObterRelatorioAcompanhamentoAsync(string protocolo, string cpf);
    }
}
