using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Resultado da vistoria presencial de campo.
    /// Etapa 4 do fluxo. Cria este registro → status avança para VISTORIA_REALIZADA.
    /// Uma ocorrência pode ter múltiplas vistorias (1:N) — cada uma com seu número sequencial.
    /// </summary>
    [Table("vistorias")]
    public class Vistoria
    {
        public int Id { get; set; }

        // FK (dependente da ocorrência — 1:N)
        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

        // ── Sequência e vínculo ──────────────────────────────────────────────────
        /// <summary>Número sequencial dentro da ocorrência (1 = primeira vistoria realizada).</summary>
        public int Numero { get; set; } = 1;

        /// <summary>Agendamento ao qual esta vistoria está vinculada (opcional).</summary>
        public int? AgendamentoId { get; set; }
        public AgendamentoVistoria? Agendamento { get; set; }

        // ── Dados temporais ───────────────────────────────────────────────────────
        public DateOnly DataVistoria { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public TimeSpan HorarioTermino { get; set; }

        // ── Caracterização do local ──────────────────────────────────────────────
        public string? DescricaoDoLocal { get; set; }
        public string? CaracterizacaoDoLocal { get; set; }
        public TipoEdificacao Edificacao { get; set; }
        public TipoEstrutura Estrutura { get; set; }

        // ── Dados da edificação ──────────────────────────────────────────────────
        public int NumeroMoradias { get; set; }
        public int NumeroComodos { get; set; }
        public int NumeroPavimentos { get; set; }
        public int NumeroMoradiasNoLote { get; set; }

        // ── Composição familiar ──────────────────────────────────────────────────
        public bool PossuiUnidadeFamiliar { get; set; }
        public int NumeroAdultos { get; set; }
        public int NumeroCriancas { get; set; }
        public int NumeroIdosos { get; set; }
        public int NumeroDeficientes { get; set; }

        /// <summary>
        /// Total de moradores. Deve ser igual à soma dos grupos acima.
        /// Armazenado para facilitar relatórios sem recalcular.
        /// </summary>
        public int TotalMoradores { get; set; }

        // ── Classificação de risco ───────────────────────────────────────────────
        public TipoRiscoVistoria TipoRisco { get; set; }
        public GrauRisco GrauRiscoEncontrado { get; set; }
        public TipificacaoOcorrencia TipificacaoOcorrencia { get; set; }
        public RegimeOcupacaoImovel RegimeOcupacao { get; set; }

        // ── Conclusões ───────────────────────────────────────────────────────────
        public string? Motivacao { get; set; }
        public AreaAfetada AreasAfetadas { get; set; }
        public TipoInterdicao Interdicao { get; set; }
        public TipoRemocao Remocao { get; set; }
        public string? Orientacoes { get; set; }

        /// <summary>Encaminhamentos imediatos registrados em campo (diferente dos encaminhamentos formais da Etapa 6).</summary>
        public string? EncaminhamentosDeCampo { get; set; }

        // ── Auditoria ────────────────────────────────────────────────────────────
        public int RegistradoPorId { get; set; }
        public Usuario RegistradoPor { get; set; } = null!;
        public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }
}
