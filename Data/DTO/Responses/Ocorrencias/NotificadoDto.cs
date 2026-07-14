namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>Pessoa que recebeu o relatório da ocorrência (notificado).</summary>
    public class NotificadoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? RgCpf { get; set; }
        public DateOnly DataNotificacao { get; set; }

        /// <summary>EMAIL ou PRESENCIAL. PRESENCIAL exige assinatura coletada.</summary>
        public string FormaRecebimento { get; set; } = "EMAIL";

        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime RegistradoEm { get; set; }
    }
}
