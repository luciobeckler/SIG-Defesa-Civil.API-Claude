using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>Resultado da vistoria presencial — resposta da Etapa 4.</summary>
    public class VistoriaDto
    {
        public int Id { get; set; }

        // Sequência e vínculo
        public int Numero { get; set; }
        public int? AgendamentoId { get; set; }

        // Temporais
        public DateOnly DataVistoria { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public TimeSpan HorarioTermino { get; set; }

        // Caracterização
        public string? DescricaoDoLocal { get; set; }
        public string? CaracterizacaoDoLocal { get; set; }
        public TipoEdificacao Edificacao { get; set; }
        public TipoEstrutura Estrutura { get; set; }

        // Edificação
        public int NumeroMoradias { get; set; }
        public int NumeroComodos { get; set; }
        public int NumeroPavimentos { get; set; }
        public int NumeroMoradiasNoLote { get; set; }

        // Composição familiar
        public bool PossuiUnidadeFamiliar { get; set; }
        public int NumeroAdultos { get; set; }
        public int NumeroCriancas { get; set; }
        public int NumeroIdosos { get; set; }
        public int NumeroDeficientes { get; set; }
        public int TotalMoradores { get; set; }

        // Classificação de risco
        public TipoRiscoVistoria TipoRisco { get; set; }
        public GrauRisco GrauRiscoEncontrado { get; set; }
        public TipificacaoOcorrencia TipificacaoOcorrencia { get; set; }
        public RegimeOcupacaoImovel RegimeOcupacao { get; set; }

        // Conclusões
        public string? Motivacao { get; set; }
        public AreaAfetada AreasAfetadas { get; set; }
        public TipoInterdicao Interdicao { get; set; }
        public TipoRemocao Remocao { get; set; }
        public string? Orientacoes { get; set; }
        public string? EncaminhamentosDeCampo { get; set; }

        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime RegistradoEm { get; set; }
    }
}
