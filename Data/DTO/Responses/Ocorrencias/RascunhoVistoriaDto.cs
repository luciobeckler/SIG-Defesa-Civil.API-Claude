namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>
    /// Preenchimento em andamento do formulário de vistoria.
    /// O conteúdo é opaco para a API: quem o interpreta é o aplicativo.
    /// </summary>
    public class RascunhoVistoriaDto
    {
        public string ConteudoJson { get; set; } = "{}";

        /// <summary>Quando foi salvo, para decidir entre o rascunho local e o do servidor.</summary>
        public DateTime AtualizadoEm { get; set; }
    }
}
