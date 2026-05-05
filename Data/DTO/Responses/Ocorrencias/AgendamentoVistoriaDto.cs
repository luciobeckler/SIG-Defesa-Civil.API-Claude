namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>Agendamento e tentativas de vistoria — resposta da Etapa 3.</summary>
    public class AgendamentoVistoriaDto
    {
        public int Id { get; set; }

        // Equipe designada
        public int Vistoriador1Id { get; set; }
        public string NomeVistoriador1 { get; set; } = string.Empty;
        public string? MatriculaVistoriador1 { get; set; }

        public int? Vistoriador2Id { get; set; }
        public string? NomeVistoriador2 { get; set; }
        public string? MatriculaVistoriador2 { get; set; }

        // Tentativas (em ordem crescente de NumeroTentativa)
        public List<TentativaVistoriaDto> Tentativas { get; set; } = new();

        public string AgendadoPor { get; set; } = string.Empty;
        public DateTime AgendadoEm { get; set; }
    }

    /// <summary>Uma tentativa de comparecimento.</summary>
    public class TentativaVistoriaDto
    {
        public int Id { get; set; }
        public int NumeroTentativa { get; set; }
        public DateTime DataHoraTentativa { get; set; }
        public string? Observacao { get; set; }
    }
}
