using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>Filtros opcionais para listagem de ocorrências.</summary>
    public class FiltroOcorrenciaDto
    {
        public StatusOcorrencia? Status { get; set; }

        /// <summary>Filtra pelo grau de risco da avaliação inicial (Etapa 2).</summary>
        public GrauRisco? GrauRiscoInicial { get; set; }

        /// <summary>Filtra apenas emergências (Etapa 2).</summary>
        public bool? Emergencia { get; set; }

        /// <summary>Filtra por vistoriador designado (Etapa 3).</summary>
        public int? VistoriadorId { get; set; }

        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? Protocolo { get; set; }
        public string? Bairro { get; set; }
    }
}
