using SIG_Defesa_Civil.API.Data.DTO.Responses.Arquivos;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Usuairos;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>
    /// DTO com dados sensíveis revelados (requer log LGPD).
    /// Retornado apenas após chamada explícita ao endpoint de revelação.
    /// </summary>
    public class OcorrenciaDadosSensiveisDto
    {
        public int Id { get; set; }
        public string Protocolo { get; set; } = string.Empty;
        public StatusOcorrencia Status { get; set; }

        // Dados completos do solicitante (SEM mascaramento)
        public CidadaoCompletoDto Solicitante { get; set; } = null!;

        // Endereço completo (sem mascaramento)
        public LocalizacaoDto? Localizacao { get; set; }

        // Resumo de risco (Etapa 2, se existir)
        public GrauRisco? GrauRiscoInicial { get; set; }
        public TipificacaoOcorrencia? TipificacaoInicial { get; set; }

        // Metadados de acesso (transparência LGPD)
        public AcessoLgpdDto UltimoAcesso { get; set; } = null!;

        public List<DocumentoVisualizacao> Documentos { get; set; } = new();
    }
}
