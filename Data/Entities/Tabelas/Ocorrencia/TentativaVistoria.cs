using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Cada tentativa de comparecimento à vistoria.
    /// Normaliza os campos DATA_TENTATIVA_1..3 e HORARIO_TENTATIVA_1..3 da planilha
    /// em linhas separadas, evitando colunas nulas e limitando N futuras tentativas.
    /// </summary>
    [Table("tentativas_vistoria")]
    public class TentativaVistoria
    {
        public int Id { get; set; }

        // FK para o agendamento pai
        public int AgendamentoId { get; set; }
        public AgendamentoVistoria Agendamento { get; set; } = null!;

        /// <summary>Sequência da tentativa dentro do agendamento (1, 2, 3…).</summary>
        public int NumeroTentativa { get; set; }

        public DateTime DataHoraTentativa { get; set; }

        /// <summary>Observação opcional sobre o motivo da não realização (ex: "Morador ausente").</summary>
        public string? Observacao { get; set; }

        public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
    }
}
