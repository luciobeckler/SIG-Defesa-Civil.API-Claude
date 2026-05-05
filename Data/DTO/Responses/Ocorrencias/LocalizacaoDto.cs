namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>Endereço e georreferenciamento retornados nas respostas (Etapa 1).</summary>
    public class LocalizacaoDto
    {
        public string Endereco { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string? Numero { get; set; }
        public string? Cep { get; set; }
        public string? Complemento { get; set; }
        public string Cidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string? Coordenada { get; set; }
        public string? Referencia { get; set; }
        public string? NumeroIptu { get; set; }
    }
}
