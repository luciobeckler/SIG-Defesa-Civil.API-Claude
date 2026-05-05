using SIG_Defesa_Civil.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// Avaliação inicial de risco realizada pelo atendente/agente — Etapa 2.
    /// Ao registrar, o status da ocorrência avança para EM_AVALIACAO.
    /// </summary>
    public class RegistrarAvaliacaoRiscoRequest
    {
        [Required] public TipificacaoOcorrencia TipificacaoInicial { get; set; }
        [Required] public GrauRisco GrauRiscoInicial { get; set; }

        /// <summary>ID do usuário agente que realiza a triagem (diferente do solicitante).</summary>
        [Required] public int AbertaPorUsuarioId { get; set; }

        /// <summary>Texto de requisição de documentos ao setor. Opcional.</summary>
        public string? RequisicaoSetorDocumento { get; set; }

        /// <summary>Marcar como emergência (atendimento prioritário).</summary>
        public bool Emergencia { get; set; } = false;
    }
}
