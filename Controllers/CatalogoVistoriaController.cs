using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Services;
using SIG_Defesa_Civil.API.Services.Vistoria;

namespace SIG_Defesa_Civil.API.Controllers
{
    /// <summary>
    /// Catálogo de opções personalizadas dos campos de seleção da vistoria.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/vistorias/opcoes")]
    [Produces("application/json")]
    public class CatalogoVistoriaController : DefesaCivilBaseController
    {
        private readonly ICatalogoVistoriaService _catalogo;
        private readonly ILogger<CatalogoVistoriaController> _logger;

        public CatalogoVistoriaController(
            ICatalogoVistoriaService catalogo,
            ILogger<CatalogoVistoriaController> logger)
        {
            _catalogo = catalogo;
            _logger = logger;
        }

        /// <summary>Lista as opções personalizadas de todos os campos.</summary>
        /// <response code="200">Lista de opções (pode ser vazia)</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<OpcaoCampoVistoriaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var resultado = await _catalogo.ListarAsync();
                return Ok(ApiResponse<List<OpcaoCampoVistoriaDto>>.Success(resultado));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, "ListarOpcoesVistoria");
            }
        }

        /// <summary>Adiciona uma opção personalizada a um campo de seleção.</summary>
        /// <response code="201">Opção criada (ou já existente)</response>
        /// <response code="422">Campo inválido ou valor vazio</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<OpcaoCampoVistoriaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Adicionar([FromBody] CriarOpcaoCampoRequest request)
        {
            try
            {
                var resultado = await _catalogo.AdicionarAsync(request);
                return StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse<OpcaoCampoVistoriaDto>.Success(resultado, "Opção adicionada ao catálogo"));
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, "AdicionarOpcaoVistoria");
            }
        }
    }
}
