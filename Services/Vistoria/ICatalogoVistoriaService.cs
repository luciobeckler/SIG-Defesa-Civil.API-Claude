using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;

namespace SIG_Defesa_Civil.API.Services.Vistoria
{
    /// <summary>
    /// Catálogo de opções personalizadas dos campos de seleção da vistoria.
    /// As opções fixas (enums) vivem no frontend; aqui ficam apenas as adicionadas em runtime.
    /// </summary>
    public interface ICatalogoVistoriaService
    {
        /// <summary>Lista todas as opções personalizadas (de todos os campos).</summary>
        Task<List<OpcaoCampoVistoriaDto>> ListarAsync();

        /// <summary>
        /// Adiciona uma opção personalizada a um campo. Idempotente: se já existir
        /// (mesmo campo + valor), retorna a existente sem duplicar.
        /// </summary>
        Task<OpcaoCampoVistoriaDto> AdicionarAsync(CriarOpcaoCampoRequest request);
    }
}
