using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Dados coletados durante a vistoria presencial — Etapa 4.
    /// Ao registrar, o status avança para VISTORIA_REALIZADA.
    /// </summary>
    public class RegistrarVistoriaRequest
    {
        /// <summary>
        /// ID do agendamento ao qual esta vistoria está vinculada (opcional).
        /// Se omitido, o serviço usa o agendamento ATIVO da ocorrência.
        /// A equipe executora é derivada dos vistoriadores designados nesse agendamento.
        /// Ao registrar, o agendamento tem seu status atualizado para CONCLUIDO.
        /// </summary>
        public int? AgendamentoId { get; set; }

        // ── Dados temporais ───────────────────────────────────────────────────────
        [Required] public DateOnly DataVistoria { get; set; }
        [Required] public TimeSpan HorarioInicio { get; set; }
        [Required] public TimeSpan HorarioTermino { get; set; }

        // ── Caracterização do local ──────────────────────────────────────────────
        public string? DescricaoDoLocal { get; set; }
        public CaracterizacaoLocal? CaracterizacaoDoLocal { get; set; }

        [Required] public TipoEdificacao Edificacao { get; set; }
        [Required] public TipoEstrutura Estrutura { get; set; }

        // ── Dados da edificação ──────────────────────────────────────────────────
        [Range(0, int.MaxValue)] public int NumeroMoradias { get; set; }
        [Range(0, int.MaxValue)] public int NumeroComodos { get; set; }
        [Range(0, int.MaxValue)] public int NumeroPavimentos { get; set; }
        [Range(0, int.MaxValue)] public int NumeroMoradiasNoLote { get; set; }

        // ── Composição familiar ──────────────────────────────────────────────────
        public bool PossuiUnidadeFamiliar { get; set; }
        [Range(0, int.MaxValue)] public int NumeroAdultos { get; set; }
        [Range(0, int.MaxValue)] public int NumeroCriancas { get; set; }
        [Range(0, int.MaxValue)] public int NumeroIdosos { get; set; }
        [Range(0, int.MaxValue)] public int NumeroDeficientes { get; set; }

        /// <summary>
        /// Total de moradores. Se não enviado, o serviço calcula como
        /// Adultos + Crianças + Idosos + Deficientes.
        /// </summary>
        public int? TotalMoradores { get; set; }

        // ── Classificação de risco ───────────────────────────────────────────────
        [Required] public TipoRiscoVistoria TipoRisco { get; set; }
        [Required] public GrauRisco GrauRiscoEncontrado { get; set; }

        /// <summary>Tipificações identificadas em campo — multi-select (mínimo 1).</summary>
        [Required, MinLength(1, ErrorMessage = "Informe ao menos uma tipificação.")]
        public List<TipificacaoOcorrencia> TipificacaoOcorrencia { get; set; } = new();

        [Required] public RegimeOcupacaoImovel RegimeOcupacao { get; set; }

        // ── Conclusões ───────────────────────────────────────────────────────────
        /// <summary>Causas/motivações — multi-select.</summary>
        public List<Motivacao> Motivacao { get; set; } = new();

        /// <summary>Áreas afetadas — multi-select (mínimo 1).</summary>
        [Required, MinLength(1, ErrorMessage = "Informe ao menos uma área afetada.")]
        public List<AreaAfetada> AreasAfetadas { get; set; } = new();

        [Required] public TipoInterdicao Interdicao { get; set; }
        [Required] public TipoRemocao Remocao { get; set; }

        /// <summary>Orientações ao morador — multi-select.</summary>
        public List<Orientacao> Orientacoes { get; set; } = new();

        /// <summary>Observações livres registradas em campo.</summary>
        public string? Observacoes { get; set; }

        /// <summary>Encaminhamentos imediatos registrados em campo — multi-select.</summary>
        public List<Encaminhamento> EncaminhamentosDeCampo { get; set; } = new();
    }
}
