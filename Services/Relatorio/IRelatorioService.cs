namespace SIG_Defesa_Civil.API.Services.Relatorio
{
    public interface IRelatorioService
    {
        /// <summary>
        /// Gera o relatório final da ocorrência preenchendo o template .docx com os dados
        /// da vistoria selecionada. Uma ocorrência possui apenas um relatório final;
        /// se já existir, o anterior é substituído.
        /// </summary>
        /// <returns>Caminho relativo do arquivo gerado.</returns>
        Task<string> GerarRelatorioAsync(int ocorrenciaId, int vistoriaId, int usuarioId);

        /// <summary>
        /// Remove o registro do relatório final da ocorrência do banco de dados.
        /// O arquivo físico permanece no storage (sem risco de dados perdidos).
        /// </summary>
        Task ExcluirRelatorioAsync(int ocorrenciaId);
    }
}
