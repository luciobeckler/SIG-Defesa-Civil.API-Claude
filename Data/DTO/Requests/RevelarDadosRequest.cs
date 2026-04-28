namespace SIG_Defesa_Civil.API.Data.DTO.Requests
{
    /// <summary>
    /// Request para revelação de dados sensíveis (endpoint específico)
    /// </summary>
    public class RevelarDadosRequest
    {
        /// <summary>
        /// ID do usuário que está solicitando o acesso (Atendente, Vistoriador, Admin)
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Justificativa obrigatória para o acesso (compliance LGPD)
        /// </summary>
        public string Justificativa { get; set; } = string.Empty;
    }
}
