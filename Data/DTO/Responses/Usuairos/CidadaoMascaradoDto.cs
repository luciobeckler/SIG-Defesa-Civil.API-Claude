namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Usuairos
{
    /// <summary>
    /// Dados do cidadão com informações sensíveis mascaradas
    /// </summary>
    public class CidadaoMascaradoDto
    {
        /// <summary>
        /// Nome mascarado: "João da Silva" -> "João d* S*****"
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// CPF mascarado: "12345678901" -> "***.***.***-01"
        /// </summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>
        /// Email mascarado: "joao@email.com" -> "j***@email.com"
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Telefone mascarado: "+5531987654321" -> "+55 31 *****-4321"
        /// </summary>
        public string Telefone { get; set; } = string.Empty;
    }
}
