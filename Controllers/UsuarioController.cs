namespace SIG_Defesa_Civil.API.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using SIG_Defesa_Civil.API.Data.DTO.Requests.Auth;
    using SIG_Defesa_Civil.API.Enums;
    using SIG_Defesa_Civil.API.Services;
    using SIG_Defesa_Civil.API.Services.Auth;

    /// <summary>
    /// Gerenciamento de usuários (servidores da Defesa Civil).
    /// Apenas o ADMIN pode criar e listar usuários.
    /// </summary>
    [Route("api/v1/usuarios")]
    [Authorize]
    public class UsuarioController : DefesaCivilBaseController
    {
        private readonly IAuthService _authService;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(IAuthService authService, ILogger<UsuarioController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos os usuários cadastrados no sistema.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var usuarios = await _authService.ListarUsuariosAsync();
                return Ok(ApiResponse<object>.Success(usuarios));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, nameof(Listar));
            }
        }

        /// <summary>
        /// Cria um novo colaborador (ATENDENTE, VISTORIADOR ou ADMIN).
        /// Cidadãos não possuem conta no sistema.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest request)
        {
            try
            {
                if (request.TipoUsuario == TipoUsuario.CIDADAO)
                    return ErroNegocio("Cidadãos não possuem conta no sistema.");

                var usuario = await _authService.CriarUsuarioAsync(request);

                return CreatedAtAction(
                    nameof(ObterPorId),
                    new { id = usuario.Id },
                    ApiResponse<object>.Success(usuario, "Usuário criado com sucesso."));
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, nameof(Criar));
            }
        }

        /// <summary>
        /// Lista apenas os vistoriadores ativos.
        /// Acessível a qualquer colaborador autenticado (necessário para agendamento).
        /// </summary>
        [HttpGet("vistoriadores")]
        [Authorize]
        public async Task<IActionResult> ListarVistoriadores()
        {
            try
            {
                var todos = await _authService.ListarUsuariosAsync();
                var vistoriadores = todos
                    .Where(u => u.TipoUsuario == TipoUsuario.VISTORIADOR || u.TipoUsuario == TipoUsuario.ADMIN)
                    .Where(u => u.Ativo);
                return Ok(ApiResponse<object>.Success(vistoriadores));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, nameof(ListarVistoriadores));
            }
        }

        /// <summary>
        /// Obtém um usuário pelo ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                var usuario = await _authService.ObterPorIdAsync(id);

                if (usuario is null)
                    return NaoEncontrado("Usuário não encontrado.");

                return Ok(ApiResponse<object>.Success(usuario));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, nameof(ObterPorId));
            }
        }

        /// <summary>
        /// Atualiza nome e e-mail de um usuário (somente ADMIN).
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarUsuarioRequest request)
        {
            try
            {
                var usuario = await _authService.AtualizarUsuarioAsync(id, request);
                return Ok(ApiResponse<object>.Success(usuario, "Usuário atualizado com sucesso."));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrado"))
            {
                return NaoEncontrado(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, nameof(Atualizar));
            }
        }

        /// <summary>
        /// Ativa ou desativa um usuário (desativação lógica — o registro nunca é excluído).
        /// Usuários desativados não conseguem entrar no sistema nem receber novas atribuições.
        /// </summary>
        [HttpPatch("{id:int}/ativo")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AlterarAtivo(int id, [FromBody] AlterarStatusUsuarioRequest request)
        {
            try
            {
                var usuario = await _authService.AlterarAtivoAsync(
                    id, request.Ativo, ObterUsuarioIdInterno());
                var acao = request.Ativo ? "reativado" : "desativado";
                return Ok(ApiResponse<object>.Success(usuario, $"Usuário {acao} com sucesso."));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrado"))
            {
                return NaoEncontrado(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErroNegocio(ex.Message);
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, nameof(AlterarAtivo));
            }
        }
    }
}
