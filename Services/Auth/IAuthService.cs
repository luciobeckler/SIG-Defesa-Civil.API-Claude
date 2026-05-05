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
    }
}
