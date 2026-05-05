using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Usuairos;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>
    /// Visão completa de uma ocorrência com todas as etapas preenchidas.
    /// Retornado pelo endpoint GET /ocorrencias/{id}.
    /// Etapas não iniciadas aparecem como null.
    /// Dados do solicitante são mascarados por padrão (LGPD).
    /// Use o endpoint de revelação para acessar dados sensíveis.
    /// </summary>
    public class OcorrenciaDetalheDto
    {
        // ── Identidade ────────────────────────────────────────────────────────────
        public int Id { get; set; }
        public string Protocolo { get; set; } = string.Empty;
        public StatusOcorrencia Status { get; set; }

        // ── Etapa 1: Abertura ─────────────────────────────────────────────────────
        public string DescricaoProblema { get; set; } = string.Empty;
        public CidadaoMascaradoDto Solicitante { get; set; } = null!;
        public LocalizacaoDto? Localizacao { get; set; }

        // ── Etapa 2: Avaliação de Risco ───────────────────────────────────────────
        /// <summary>Null enquanto a Etapa 2 não for preenchida.</summary>
        public AvaliacaoRiscoDto? AvaliacaoRisco { get; set; }

        // ── Etapa 3: Agendamento de Vistoria ──────────────────────────────────────
        /// <summary>Null enquanto a Etapa 3 não for preenchida.</summary>
        public AgendamentoVistoriaDto? AgendamentoVistoria { get; set; }

        // ── Etapa 4: Vistoria Presencial ──────────────────────────────────────────
        /// <summary>Null enquanto a Etapa 4 não for preenchida.</summary>
        public VistoriaDto? Vistoria { get; set; }

        // ── Etapa 5: Notificações ─────────────────────────────────────────────────
        /// <summary>Lista vazia enquanto a Etapa 5 não for preenchida.</summary>
        public List<NotificadoDto> Notificados { get; set; } = new();

        // ── Etapa 6: Encaminhamento Final ─────────────────────────────────────────
        /// <summary>Null enquanto a Etapa 6 não for preenchida.</summary>
        public EncaminhamentoFinalDto? EncaminhamentoFinal { get; set; }

        // ── Arquivos ──────────────────────────────────────────────────────────────
        public List<DocumentoVisualizacao> Arquivos { get; set; } = new();

        // ── Auditoria ─────────────────────────────────────────────────────────────
        public string CriadoPor { get; set; } = string.Empty;
        public DateTime AbertaEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }
}
