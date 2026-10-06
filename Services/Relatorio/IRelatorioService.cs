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
        /// Monta a ficha de vistoria (o "Registro de Ocorrência" oficial) preenchida com
        /// o que já se sabe da ocorrência, para a equipe imprimir e levar a campo.
        ///
        /// Diferente do relatório final, <b>nada é gravado</b>: não salva no storage nem
        /// cria linha em <c>arquivos</c>. É um rascunho descartável, baixável em qualquer
        /// fase — o que ainda não foi preenchido sai em branco, para completar à mão.
        /// </summary>
        /// <returns>O .docx em memória.</returns>
        Task<byte[]> GerarFichaVistoriaAsync(int ocorrenciaId);

        /// <summary>
        /// Remove o registro do relatório final da ocorrência do banco de dados.
        /// O arquivo físico permanece no storage (sem risco de dados perdidos).
        /// </summary>
        Task ExcluirRelatorioAsync(int ocorrenciaId);
    }
}
