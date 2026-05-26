namespace SIG_Defesa_Civil.API.Services.Auth
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using SIG_Defesa_Civil.API.Data.Configuration.Auth;
    using SIG_Defesa_Civil.API.Data.DTO.Requests.Auth;
    using SIG_Defesa_Civil.API.Data.DTO.Responses.Auth;
    using SIG_Defesa_Civil.API.Data.Models;
    using SIG_Defesa_Civil.API.Data.Models.Tabelas;
    using SIG_Defesa_Civil.API.Enums;

    public class AuthService : IAuthService
    {
        private readonly DefesaCivilContext _context;
        private readonly IPasswordHasher<Usuario> _hasher;
        private readonly JwtSettings _jwt;

        public AuthService(
            DefesaCivilContext context,
            IPasswordHasher<Usuario> hasher,
            IOptions<JwtSettings> jwt)
        {
            _context = context;
            _hasher = hasher;
            _jwt = jwt.Value;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequest request)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Ativo);

            if (usuario is null || string.IsNullOrEmpty(usuario.SenhaHash))
                return null;

            var result = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, request.Senha);
            if (result == PasswordVerificationResult.Failed)
                return null;

            var expiration = DateTime.UtcNow.AddHours(_jwt.ExpirationHours);
            var token = GerarToken(usuario, expiration);

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiration,
                Usuario = MapearDto(usuario)
            };
        }

        public async Task<UsuarioResponseDto> CriarUsuarioAsync(CriarUsuarioRequest request)
        {
            if (request.TipoUsuario != TipoUsuario.CIDADAO && string.IsNullOrWhiteSpace(request.Matricula))
                throw new InvalidOperationException("A matrícula é obrigatória para usuários não-cidadãos.");

            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == request.Email);

            if (emailExiste)
                throw new InvalidOperationException($"Já existe um usuário com o e-mail '{request.Email}'.");

            var usuario = new Usuario
            {
                Nome = request.Nome,
                Email = request.Email,
                TipoUsuario = request.TipoUsuario,
                Matricula = request.Matricula,
                Cpf = request.Cpf,
                Rg = request.Rg,
                OrgaoEmissor = request.OrgaoEmissor,
                Telefone = request.Telefone,
                Celular = request.Celular,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            };

            usuario.SenhaHash = _hasher.HashPassword(usuario, request.Senha);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return MapearDto(usuario);
        }

        public async Task<IEnumerable<UsuarioResponseDto>> ListarUsuariosAsync()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.Nome)
                .ToListAsync();

            return usuarios.Select(MapearDto);
        }

        public async Task<UsuarioResponseDto?> ObterPorIdAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            return usuario is null ? null : MapearDto(usuario);
        }

        // ── Privados ──────────────────────────────────────────────────────────────

        private string GerarToken(Usuario usuario, DateTime expiration)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.TipoUsuario.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static UsuarioResponseDto MapearDto(Usuario u) => new()
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            TipoUsuario = u.TipoUsuario,
            Matricula = u.Matricula,
            Ativo = u.Ativo,
            CriadoEm = u.CriadoEm
        };
    }
}
