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

        // Documentos pessoais (PII — sujeito a mascaramento LGPD)
        public string? Cpf { get; set; }
        public string? Rg { get; set; }
        /// <summary>Órgão emissor do RG (ex: SSP/MG, DETRAN/SP).</summary>
        public string? OrgaoEmissor { get; set; }

        // Contatos
        public string? Telefone { get; set; }
        /// <summary>Número de celular — campo separado de Telefone (fixo).</summary>
        public string? Celular { get; set; }

        // Dados funcionais (apenas para ATENDENTE / VISTORIADOR / ADMIN)
        /// <summary>Matrícula funcional. Preenchido apenas para servidores.</summary>
        public string? Matricula { get; set; }

        // Autenticação
        /// <summary>Hash PBKDF2 da senha (gerenciado por IPasswordHasher). Nulo para cidadãos.</summary>
        public string? SenhaHash { get; set; }

        public TipoUsuario TipoUsuario { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
