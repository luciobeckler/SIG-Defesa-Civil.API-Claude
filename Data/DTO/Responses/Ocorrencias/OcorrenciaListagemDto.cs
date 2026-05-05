using SIG_Defesa_Civil.API.Data.DTO.Responses.Usuairos;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>
    /// DTO de listagem — dados mascarados (LGPD) + resumo de cada etapa.
    /// Use OcorrenciaDetalheDto para o registro completo.
    /// </summary>
    public class OcorrenciaListagemDto
    {
        // ── Identidade ────────────────────────────────────────────────────────────
        public int Id { get; set; }
        public string Protocolo { get; set; } = string.Empty;
        public StatusOcorrencia Status { get; set; }

        // ── Etapa 1 (mascarado — LGPD) ────────────────────────────────────────────
        public CidadaoMascaradoDto Solicitante { get; set; } = null!;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;

        // ── Etapa 2 (resumo) ──────────────────────────────────────────────────────
        public GrauRisco? GrauRiscoInicial { get; set; }
        public TipificacaoOcorrencia? TipificacaoInicial { get; set; }
        public bool? Emergencia { get; set; }

        // ── Etapa 3 (resumo) ──────────────────────────────────────────────────────
        public string? NomeVistoriador1 { get; set; }

        // ── Auditoria ─────────────────────────────────────────────────────────────
        public DateTime AbertaEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
        public int QuantidadeArquivos { get; set; }
    }
}
