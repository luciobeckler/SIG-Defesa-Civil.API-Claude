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
        // Campos de classificação armazenados como texto para aceitar opções
        // personalizadas do catálogo (além dos valores fixos dos enums).
        public string? DescricaoDoLocal { get; set; }

        /// <summary>Geomorfologia do terreno. Valor do enum CaracterizacaoLocal ou opção personalizada.</summary>
        public string? CaracterizacaoDoLocal { get; set; }

        public string Edificacao { get; set; } = string.Empty;
        public string Estrutura { get; set; } = string.Empty;

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
        public string TipoRisco { get; set; } = string.Empty;
        public string GrauRiscoEncontrado { get; set; } = string.Empty;

        /// <summary>Multi-select: tipificações identificadas na vistoria (text[]).</summary>
        public List<string> TipificacaoOcorrencia { get; set; } = new();

        public string RegimeOcupacao { get; set; } = string.Empty;

        // ── Conclusões ───────────────────────────────────────────────────────────
        /// <summary>Causas identificadas — multi-select (text[]).</summary>
        public List<string> Motivacao { get; set; } = new();

        /// <summary>Áreas do imóvel/entorno afetadas — multi-select (text[]).</summary>
        public List<string> AreasAfetadas { get; set; } = new();

        public string Interdicao { get; set; } = string.Empty;
        public string Remocao { get; set; } = string.Empty;

        /// <summary>Orientações ao morador — multi-select (text[]).</summary>
        public List<string> Orientacoes { get; set; } = new();

        /// <summary>Observações livres registradas em campo.</summary>
        public string? Observacoes { get; set; }

        /// <summary>
        /// Encaminhamentos imediatos registrados em campo — multi-select (text[]).
        /// Aceita valores do enum <see cref="Encaminhamento"/> e opções personalizadas.
        /// </summary>
        public List<string> EncaminhamentosDeCampo { get; set; } = new();

        // ── Equipe que realizou a vistoria ───────────────────────────────────────
        /// <summary>Vistoriador principal — obrigatório ao registrar a vistoria.</summary>
        public int Vistoriador1Id { get; set; }
        public Usuario Vistoriador1 { get; set; } = null!;

        /// <summary>Segundo vistoriador (opcional).</summary>
        public int? Vistoriador2Id { get; set; }
        public Usuario? Vistoriador2 { get; set; }

        // ── Auditoria ────────────────────────────────────────────────────────────
        public int RegistradoPorId { get; set; }
        public Usuario RegistradoPor { get; set; } = null!;
        public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }
}
