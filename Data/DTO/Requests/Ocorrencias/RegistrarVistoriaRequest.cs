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
        // ── Dados temporais ───────────────────────────────────────────────────────
        [Required] public DateOnly DataVistoria { get; set; }
        [Required] public TimeSpan HorarioInicio { get; set; }
        [Required] public TimeSpan HorarioTermino { get; set; }

        // ── Caracterização do local ──────────────────────────────────────────────
        public string? DescricaoDoLocal { get; set; }
        public string? CaracterizacaoDoLocal { get; set; }

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
        [Required] public TipificacaoOcorrencia TipificacaoOcorrencia { get; set; }
        [Required] public RegimeOcupacaoImovel RegimeOcupacao { get; set; }

        // ── Conclusões ───────────────────────────────────────────────────────────
        public string? Motivacao { get; set; }
        [Required] public AreaAfetada AreasAfetadas { get; set; }
        [Required] public TipoInterdicao Interdicao { get; set; }
        [Required] public TipoRemocao Remocao { get; set; }
        public string? Orientacoes { get; set; }
        public string? EncaminhamentosDeCampo { get; set; }
    }
}
