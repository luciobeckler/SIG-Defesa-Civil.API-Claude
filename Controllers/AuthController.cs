namespace SIG_Defesa_Civil.API.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using SIG_Defesa_Civil.API.Data.DTO.Requests.Auth;
    using SIG_Defesa_Civil.API.Enums;
    using SIG_Defesa_Civil.API.Services;
    using SIG_Defesa_Civil.API.Services.Auth;

    /// <summary>
    /// Autenticação de colaboradores da Defesa Civil.
    /// Cidadãos não possuem conta — este controller é exclusivo para servidores.
    /// </summary>
    [Route("api/v1/auth")]
    public class AuthController : DefesaCivilBaseController
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Autentica um colaborador (ATENDENTE, VISTORIADOR ou ADMIN) e retorna um JWT.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);

                if (response is null)
                    return Unauthorized(ApiResponse<object>.Error(
                        "E-mail ou senha inválidos.",
                        ErrosRequisicoes.ACESSO_NEGADO));

                return Ok(ApiResponse<object>.Success(response, "Login realizado com sucesso."));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, nameof(Login));
            }
        }

        /// <summary>
        /// Retorna os dados do colaborador autenticado (a partir do token JWT).
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            try
            {
                var id = ObterUsuarioIdInterno();
                var usuario = await _authService.ObterPorIdAsync(id);

                if (usuario is null)
                    return NaoEncontrado("Usuário não encontrado.");

                return Ok(ApiResponse<object>.Success(usuario));
            }
            catch (Exception ex)
            {
                return ErroInterno(ex, _logger, nameof(Me));
            }
        }

        /// <summary>
        /// Logout — JWT é stateless; o cliente descarta o token.
        /// Para revogação real, implemente blocklist ou refresh tokens.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(ApiResponse<object>.Success(null, "Logout realizado com sucesso."));
        }
    }
}
