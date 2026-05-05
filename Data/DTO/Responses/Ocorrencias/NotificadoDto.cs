namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>Pessoa notificada — resposta da Etapa 5.</summary>
    public class NotificadoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? RgCpf { get; set; }
        public DateOnly DataNotificacao { get; set; }
        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime RegistradoEm { get; set; }
    }
}
