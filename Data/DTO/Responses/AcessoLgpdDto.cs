namespace SIG_Defesa_Civil.API.Data.DTO.Responses
{
    /// <summary>
    /// Informações sobre o acesso aos dados sensíveis
    /// </summary>
    public class AcessoLgpdDto
    {
        public string UsuarioQueAcessou { get; set; } = string.Empty;
        public DateTime DataHoraAcesso { get; set; }
        public string IpOrigem { get; set; } = string.Empty;
    }
}
