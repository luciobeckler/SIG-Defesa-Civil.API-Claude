using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;

namespace SIG_Defesa_Civil.API.Data.Models.Tabelas
{
    /// <summary>
    /// Entidade que representa um arquivo armazenado no file system local.
    /// Armazena apenas o caminho RELATIVO - o caminho absoluto é calculado em runtime.
    /// </summary>
    public class Arquivo
    {
        public int Id { get; set; }
        public int OcorrenciaId { get; set; }
        public string NomeOriginal { get; set; } = string.Empty;

        /// <summary>
        /// Tipo/categoria do arquivo
        /// Valores: FOTO_CIDADAO, COMPROVANTE_RESIDENCIA, FICHA_VISTORIA, FOTO_CAMPO, RELATORIO_FINAL
        /// </summary>
        public string TipoArquivo { get; set; } = string.Empty;

        /// <summary>
        /// Caminho RELATIVO do arquivo no armazenamento.
        /// Formato: /{PROTOCOLO}/Documentos/{nome_arquivo_unico.ext}
        /// Exemplo: /2026-0001/Documentos/FOTO_CIDADAO_a1b2c3d4.jpg
        /// 
        /// CRÍTICO: Este é um caminho relativo à pasta base configurada em appsettings.
        /// O caminho absoluto só é calculado em runtime pelo IStorageService.
        /// </summary>
        public string CaminhoRelativo { get; set; } = string.Empty;
        public long TamanhoBytes { get; set; }

        /// <summary>
        /// Colaborador que enviou o arquivo. Nulo nos anexos da abertura pública,
        /// enviados pelo próprio cidadão — que não possui conta no sistema.
        /// </summary>
        public int? EnviadoPorUserId { get; set; }

        public DateTime EnviadoEm { get; set; }

        // Navegação
        public Ocorrencia Ocorrencia { get; set; } = null!;
        public Usuario? Usuario { get; set; }
    }
}
