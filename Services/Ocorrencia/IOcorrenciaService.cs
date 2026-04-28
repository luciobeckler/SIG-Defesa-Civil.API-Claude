using SIG_Defesa_Civil.API.Data.DTO.Requests;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.Ocorrencia
{
    /// <summary>
    /// Serviço de lógica de negócio para gestão de ocorrências
    /// </summary>
    public interface IOcorrenciaService
    {
        /// <summary>
        /// Cria uma nova ocorrência com transação coordenada entre PostgreSQL e SharePoint.
        /// Regra "All-or-Nothing": Se o SharePoint falhar, faz rollback do banco.
        /// </summary>
        Task<OcorrenciaCriadaDto> CriarOcorrenciaAsync(CriarOcorrenciaRequest request);

        /// <summary>
        /// Lista ocorrências com dados sensíveis mascarados (compliance LGPD).
        /// Não grava log de acesso, pois os dados estão protegidos.
        /// </summary>
        /// <param name="filtros">Filtros opcionais (status, data, etc)</param>
        /// <param name="paginacao">Parâmetros de paginação</param>
        /// <returns>Lista de ocorrências com dados mascarados</returns>
        Task<List<OcorrenciaListagemDto>> ListarOcorrenciasMascaradasAsync(
            FiltroOcorrenciaDto? filtros = null,
            PaginacaoDto? paginacao = null);

        /// <summary>
        /// Revela dados sensíveis de uma ocorrência específica.
        /// CRÍTICO: Grava registro de acesso na tabela log_acesso_lgpd.
        /// </summary>
        /// <param name="ocorrenciaId">ID da ocorrência</param>
        /// <param name="request">Dados do usuário que está acessando + justificativa</param>
        /// <param name="ipOrigem">IP de origem da requisição</param>
        /// <returns>Dados completos sem mascaramento</returns>
        Task<OcorrenciaDadosSensiveisDto> RevelarDadosSensiveisAsync(
            int ocorrenciaId,
            RevelarDadosRequest request,
            string ipOrigem);

        /// <summary>
        /// Gera documentos Word em lote usando templates pré-definidos.
        /// Para cada ocorrência, substitui tags no template e faz upload no SharePoint.
        /// Continua o processamento mesmo se uma geração falhar.
        /// </summary>
        /// <param name="request">Lista de IDs e nome do template</param>
        /// <returns>Resultado com sucessos e falhas detalhados</returns>
        Task<GeracaoLoteResultadoDto> GerarDocumentosEmLoteAsync(
            GerarDocumentosLoteRequest request);
    }
}
