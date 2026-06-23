namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>Opção personalizada de um campo de seleção da vistoria.</summary>
    public class OpcaoCampoVistoriaDto
    {
        public string Campo { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
