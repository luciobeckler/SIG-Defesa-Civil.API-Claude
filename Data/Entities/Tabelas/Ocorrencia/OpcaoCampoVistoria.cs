using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Opção personalizada adicionada pelo usuário a um campo de seleção da vistoria.
    /// Estende em runtime os catálogos que originalmente eram enums fixos
    /// (tipificação, áreas afetadas, etc.). Uma vez criada, fica disponível
    /// para todos os usuários naquele campo.
    /// </summary>
    [Table("opcoes_campo_vistoria")]
    public class OpcaoCampoVistoria
    {
        public int Id { get; set; }

        /// <summary>Chave do campo (ex.: AREA_AFETADA, TIPIFICACAO). Ver CamposVistoria.</summary>
        public string Campo { get; set; } = string.Empty;

        /// <summary>Valor armazenado na vistoria quando esta opção é selecionada.</summary>
        public string Valor { get; set; } = string.Empty;

        /// <summary>Rótulo exibido ao usuário.</summary>
        public string Label { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
