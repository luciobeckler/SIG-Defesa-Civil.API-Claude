namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Auth
{
    using System.ComponentModel.DataAnnotations;

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres.")]
        public string Senha { get; set; } = null!;
    }
}
