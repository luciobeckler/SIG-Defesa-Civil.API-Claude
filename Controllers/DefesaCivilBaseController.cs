using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Services;

namespace SIG_Defesa_Civil.API.Controllers
{
    /// <summary>
    /// Controller base com helpers de IP e identificação de usuário autenticado via JWT.
    /// </summary>
    [ApiController]
    public abstract class DefesaCivilBaseController : ControllerBase
    {
        /// <summary>
        /// Lê o ID do colaborador autenticado a partir das Claims do JWT.
        /// ⚠️ Requer que o endpoint tenha [Authorize].
        /// </summary>
        protected int ObterUsuarioIdInterno()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtClaimsTypes.Sub);

            return int.TryParse(sub, out var id) && id > 0
                ? id
                : throw new UnauthorizedAccessException(
                    "Token JWT ausente ou inválido. Este endpoint requer autenticação.");
        }

        // Alias para o claim "sub" padronizado no JWT
        private static class JwtClaimsTypes
        {
            public const string Sub = "sub";
        }

        protected string ObterIpCliente()
        {
            var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
                return forwarded.Split(',')[0].Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";
        }

        protected IActionResult ErroNegocio(string mensagem) =>
            UnprocessableEntity(ApiResponse<object>.Error(mensagem, ErrosRequisicoes.DADOS_INVALIDOS));

        protected IActionResult NaoEncontrado(string mensagem) =>
            NotFound(ApiResponse<object>.Error(mensagem, ErrosRequisicoes.DADOS_AUSENTES));

        protected IActionResult ErroInterno(Exception ex, ILogger logger, string contexto)
        {
            logger.LogError(ex, "Erro em {Contexto}", contexto);
            return StatusCode(500, ApiResponse<object>.Error(
                "Erro interno. Tente novamente.", ErrosRequisicoes.ERRO_INTERNO));
        }
    }
}
