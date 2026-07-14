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

        // Caracterização (campos de seleção = texto: enums + opções personalizadas)
        public string? DescricaoDoLocal { get; set; }
        public string? CaracterizacaoDoLocal { get; set; }
        public string Edificacao { get; set; } = string.Empty;
        public string Estrutura { get; set; } = string.Empty;

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
        public string TipoRisco { get; set; } = string.Empty;
        public string GrauRiscoEncontrado { get; set; } = string.Empty;
        public List<string> TipificacaoOcorrencia { get; set; } = new();
        public string RegimeOcupacao { get; set; } = string.Empty;

        // Conclusões
        public List<string> Motivacao { get; set; } = new();
        public List<string> AreasAfetadas { get; set; } = new();
        public string Interdicao { get; set; } = string.Empty;
        public string Remocao { get; set; } = string.Empty;
        public List<string> Orientacoes { get; set; } = new();
        public string? Observacoes { get; set; }
        public List<string> EncaminhamentosDeCampo { get; set; } = new();

        // Equipe executora
        public string NomeVistoriador1 { get; set; } = string.Empty;
        public string? MatriculaVistoriador1 { get; set; }
        public string? NomeVistoriador2 { get; set; }
        public string? MatriculaVistoriador2 { get; set; }
        public string? NomeVistoriador3 { get; set; }
        public string? MatriculaVistoriador3 { get; set; }
        public string? NomeVistoriador4 { get; set; }
        public string? MatriculaVistoriador4 { get; set; }

        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime RegistradoEm { get; set; }
    }
}
