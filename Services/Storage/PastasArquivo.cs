using System.Text;
using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Services.Storage
{
    /// <summary>
    /// Estrutura de pastas da ocorrência no storage — espelha as categorias da
    /// Central de Documentos. Cada tipo de arquivo tem sua própria pasta e o
    /// usuário pode criar pastas adicionais (ex.: "Retorno").
    /// </summary>
    public static class PastasArquivo
    {
        /// <summary>Pasta de cada tipo padrão (nomes seguros p/ filesystem e S3).</summary>
        public static readonly IReadOnlyDictionary<TipoArquivo, string> PorTipo =
            new Dictionary<TipoArquivo, string>
            {
                [TipoArquivo.FOTO_CIDADAO]           = "Fotos_do_Cidadao",
                [TipoArquivo.COMPROVANTE_RESIDENCIA] = "Comprovantes_de_Residencia",
                [TipoArquivo.FICHA_VISTORIA]         = "Fichas_de_Vistoria",
                [TipoArquivo.FOTO_CAMPO]             = "Fotos_de_Campo",
                [TipoArquivo.RELATORIO_FINAL]        = "Relatorios_Finais",
                [TipoArquivo.RELATORIO_ASSINADO]     = "Relatorios_Assinados",
                [TipoArquivo.ASSINATURA_MUNICIPIO]   = "Assinaturas",
            };

        public static string De(TipoArquivo tipo) => PorTipo[tipo];

        /// <summary>Todas as pastas padrão, na ordem de exibição da Central.</summary>
        public static IEnumerable<string> Padrao => PorTipo.Values;

        /// <summary>
        /// Sanitiza o nome de uma pasta personalizada: remove acentos e caracteres
        /// perigosos, troca espaços por "_" e limita a 50 caracteres (limite da
        /// coluna TipoArquivo). Lança se o resultado ficar vazio.
        /// </summary>
        public static string SanitizarNome(string nome)
        {
            var semAcento = new string(nome.Normalize(NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                            != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());

            var limpo = new StringBuilder();
            foreach (var c in semAcento.Trim())
            {
                if (char.IsLetterOrDigit(c) || c is '-' or '_')
                    limpo.Append(c);
                else if (char.IsWhiteSpace(c))
                    limpo.Append('_');
            }

            var resultado = limpo.ToString().Trim('_');
            if (resultado.Length > 50) resultado = resultado[..50];

            if (string.IsNullOrWhiteSpace(resultado))
                throw new InvalidOperationException($"Nome de pasta inválido: '{nome}'.");

            return resultado;
        }
    }
}
