namespace SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias
{
    /// <summary>
    /// Contagens de ocorrências por status, respeitando os mesmos filtros da
    /// listagem. Alimenta os contadores das abas, os cabeçalhos das colunas do
    /// kanban e a paginação do histórico.
    /// </summary>
    public class ResumoOcorrenciasDto
    {
        /// <summary>Total de ocorrências que atendem aos filtros.</summary>
        public int Total { get; set; }

        /// <summary>Em andamento (tudo que não é ENCERRADA/CANCELADA).</summary>
        public int Ativas { get; set; }

        /// <summary>Arquivo (ENCERRADA + CANCELADA).</summary>
        public int Arquivo { get; set; }

        /// <summary>Contagem por status (chave = nome do StatusOcorrencia).</summary>
        public Dictionary<string, int> PorStatus { get; set; } = new();
    }
}
