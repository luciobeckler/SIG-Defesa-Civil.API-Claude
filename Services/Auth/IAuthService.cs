namespace SIG_Defesa_Civil.API.Services.Auth
{
    using SIG_Defesa_Civil.API.Data.DTO.Requests.Auth;
    using SIG_Defesa_Civil.API.Data.DTO.Responses.Auth;

    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequest request);
        Task<UsuarioResponseDto> CriarUsuarioAsync(CriarUsuarioRequest request);
        Task<IEnumerable<UsuarioResponseDto>> ListarUsuariosAsync();
        Task<UsuarioResponseDto?> ObterPorIdAsync(int id);

        /// <summary>Atualiza nome e e-mail de um usuário (ação do ADMIN).</summary>
        Task<UsuarioResponseDto> AtualizarUsuarioAsync(int id, AtualizarUsuarioRequest request);

        /// <summary>
        /// Ativa/desativa um usuário (desativação lógica — nunca deleção).
        /// Impede desativar a si próprio e o último ADMIN ativo.
        /// </summary>
        Task<UsuarioResponseDto> AlterarAtivoAsync(int id, bool ativo, int usuarioLogadoId);
    }
}
