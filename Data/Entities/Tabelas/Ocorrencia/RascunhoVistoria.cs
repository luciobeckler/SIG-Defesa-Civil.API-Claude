using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Vistoria em preenchimento, salva campo a campo antes de ser registrada.
    ///
    /// Existe porque o formulário de campo é longo e o aplicativo pode ser
    /// recarregado no meio (o sistema operacional encerra o app em segundo plano,
    /// a sessão expira, o vistoriador troca de tela). Sem isto, tudo o que foi
    /// digitado se perde.
    ///
    /// É rascunho, não vistoria: não altera o status da ocorrência, não exige
    /// campos obrigatórios e é apagado quando a vistoria é registrada de fato.
    /// Um rascunho por ocorrência e vistoriador, para duas pessoas da equipe não
    /// sobrescreverem o preenchimento uma da outra.
    /// </summary>
    [Table("rascunhos_vistoria")]
    public class RascunhoVistoria
    {
        public int Id { get; set; }

        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

        /// <summary>Quem está preenchendo.</summary>
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        /// <summary>
        /// Valores do formulário como JSON. Fica opaco de propósito: o rascunho
        /// acompanha o formulário do aplicativo sem exigir migração a cada campo
        /// novo, e nada aqui é lido pelo restante do sistema.
        /// </summary>
        public string ConteudoJson { get; set; } = "{}";

        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }
}
