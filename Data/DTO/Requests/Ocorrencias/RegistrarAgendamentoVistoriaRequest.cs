using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Designação da equipe de vistoriadores — Etapa 3.
    /// Ao registrar, o status avança para VISTORIA_SOLICITADA.
    /// As tentativas de comparecimento são adicionadas separadamente via AdicionarTentativaRequest.
    /// </summary>
    public class RegistrarAgendamentoVistoriaRequest
    {
        /// <summary>ID do vistoriador principal (obrigatório).</summary>
        [Required] public int Vistoriador1Id { get; set; }

        /// <summary>ID do segundo vistoriador. Duplas são o padrão operacional.</summary>
        public int? Vistoriador2Id { get; set; }

        /// <summary>Primeira tentativa de data/hora para a visita.</summary>
        [Required] public DateTime DataHoraPrimeiraTentativa { get; set; }

        /// <summary>Observação opcional sobre o agendamento.</summary>
        public string? Observacao { get; set; }
    }

    /// <summary>
    /// Registro de uma tentativa adicional de comparecimento dentro de um agendamento.
    /// </summary>
    public class AdicionarTentativaRequest
    {
        [Required] public DateTime DataHoraTentativa { get; set; }

        /// <summary>Motivo da não realização na tentativa anterior (ex: "Morador ausente").</summary>
        public string? Observacao { get; set; }
    }
}
