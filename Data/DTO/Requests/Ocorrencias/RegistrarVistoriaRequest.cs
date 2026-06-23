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
        // Campos de seleção são texto: aceitam valores dos enums e opções personalizadas.
        public string? DescricaoDoLocal { get; set; }
        public string? CaracterizacaoDoLocal { get; set; }

        [Required] public string Edificacao { get; set; } = string.Empty;
        [Required] public string Estrutura { get; set; } = string.Empty;

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
        [Required] public string TipoRisco { get; set; } = string.Empty;
        [Required] public string GrauRiscoEncontrado { get; set; } = string.Empty;

        /// <summary>Tipificações identificadas em campo — multi-select (mínimo 1).</summary>
        [Required, MinLength(1, ErrorMessage = "Informe ao menos uma tipificação.")]
        public List<string> TipificacaoOcorrencia { get; set; } = new();

        [Required] public string RegimeOcupacao { get; set; } = string.Empty;

        // ── Conclusões ───────────────────────────────────────────────────────────
        /// <summary>Causas/motivações — multi-select.</summary>
        public List<string> Motivacao { get; set; } = new();

        /// <summary>Áreas afetadas — multi-select (mínimo 1).</summary>
        [Required, MinLength(1, ErrorMessage = "Informe ao menos uma área afetada.")]
        public List<string> AreasAfetadas { get; set; } = new();

        [Required] public string Interdicao { get; set; } = string.Empty;
        [Required] public string Remocao { get; set; } = string.Empty;

        /// <summary>Orientações ao morador — multi-select.</summary>
        public List<string> Orientacoes { get; set; } = new();

        /// <summary>Observações livres registradas em campo.</summary>
        public string? Observacoes { get; set; }

        /// <summary>Encaminhamentos imediatos registrados em campo — multi-select.</summary>
        public List<string> EncaminhamentosDeCampo { get; set; } = new();
    }
}
