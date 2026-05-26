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
        public CaracterizacaoLocal? CaracterizacaoDoLocal { get; set; }
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
        public List<TipificacaoOcorrencia> TipificacaoOcorrencia { get; set; } = new();
        public RegimeOcupacaoImovel RegimeOcupacao { get; set; }

        // Conclusões
        public List<Motivacao> Motivacao { get; set; } = new();
        public List<AreaAfetada> AreasAfetadas { get; set; } = new();
        public TipoInterdicao Interdicao { get; set; }
        public TipoRemocao Remocao { get; set; }
        public List<Orientacao> Orientacoes { get; set; } = new();
        public string? Observacoes { get; set; }
        public List<Encaminhamento> EncaminhamentosDeCampo { get; set; } = new();

        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime RegistradoEm { get; set; }
    }
}
