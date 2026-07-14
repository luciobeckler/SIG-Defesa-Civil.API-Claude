using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Atribuição da equipe de vistoriadores a um agendamento — passo posterior ao
    /// agendamento (Etapa 3). Designa quem fará a visita, permitindo que esses
    /// vistoriadores baixem a ocorrência para uso offline antes de ir a campo.
    /// </summary>
    public class AtribuirVistoriadoresRequest
    {
        /// <summary>Vistoriador principal designado (obrigatório).</summary>
        [Required] public int Vistoriador1Id { get; set; }

        /// <summary>Segundo vistoriador (opcional).</summary>
        public int? Vistoriador2Id { get; set; }

        /// <summary>Terceiro vistoriador (opcional) — equipes de até 4 pessoas.</summary>
        public int? Vistoriador3Id { get; set; }

        /// <summary>Quarto vistoriador (opcional).</summary>
        public int? Vistoriador4Id { get; set; }
    }
}
