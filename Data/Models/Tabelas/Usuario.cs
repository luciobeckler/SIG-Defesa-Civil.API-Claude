namespace SIG_Defesa_Civil.API.Data.Models.Tabelas
{
    using SIG_Defesa_Civil.API.Enums;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("usuarios")]
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Cpf { get; set; }
        public string? Rg { get; set; }
        public string? Telefone { get; set; }
        public TipoUsuario TipoUsuario { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
