using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Auth
{
    /// <summary>Edição de dados básicos de um usuário pelo ADMIN.</summary>
    public class AtualizarUsuarioRequest
    {
        [Required] public string Nome { get; set; } = string.Empty;

        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    }

    /// <summary>Ativa ou desativa um usuário (desativação lógica — nunca deleção).</summary>
    public class AlterarStatusUsuarioRequest
    {
        [Required] public bool Ativo { get; set; }
    }
}
