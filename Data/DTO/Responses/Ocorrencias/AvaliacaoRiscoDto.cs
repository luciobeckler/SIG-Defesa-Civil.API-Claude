using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>Avaliação inicial de risco — resposta da Etapa 2.</summary>
    public class AvaliacaoRiscoDto
    {
        public int Id { get; set; }
        public TipificacaoOcorrencia TipificacaoInicial { get; set; }
        public GrauRisco GrauRiscoInicial { get; set; }
        public string? NomeAgenteTriage { get; set; }
        public string? RequisicaoSetorDocumento { get; set; }
        public bool Emergencia { get; set; }
        public DateTime RegistradoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }
}
