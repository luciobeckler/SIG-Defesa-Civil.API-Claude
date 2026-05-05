namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Auth
{
    using System.ComponentModel.DataAnnotations;
    using SIG_Defesa_Civil.API.Enums;

    public class CriarUsuarioRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        public string Nome { get; set; } = null!;

        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres.")]
        public string Senha { get; set; } = null!;

        [Required(ErrorMessage = "Tipo de usuário é obrigatório.")]
        public TipoUsuario TipoUsuario { get; set; }

        /// <summary>Matrícula funcional (obrigatória para ATENDENTE, VISTORIADOR e ADMIN).</summary>
        public string? Matricula { get; set; }

        public string? Cpf { get; set; }
        public string? Rg { get; set; }
        public string? OrgaoEmissor { get; set; }
        public string? Telefone { get; set; }
        public string? Celular { get; set; }
    }
}
