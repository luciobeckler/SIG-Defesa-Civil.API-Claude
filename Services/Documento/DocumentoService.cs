using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using SIG_Defesa_Civil.API.Data.Configuration.DocumentTemplate;
using System.Text.RegularExpressions;

namespace SIG_Defesa_Civil.API.Services.Documento
{
    public class DocumentoService : IDocumentoService
    {
        private readonly TemplateSettings _settings;
        private readonly ILogger<DocumentoService> _logger;

        // Regex para encontrar tags no formato <<CAMPO>>
        private static readonly Regex TagRegex = new Regex(
            @"<<([A-Z_][A-Z0-9_]*)>>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public DocumentoService(
            IOptions<TemplateSettings> settings,
            ILogger<DocumentoService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Preenche template Word substituindo tags pelos valores fornecidos
        /// </summary>
        public async Task<Stream> PreencherTemplateAsync(
            Stream templateStream,
            Dictionary<string, string> dados)
        {
            _logger.LogInformation("Iniciando preenchimento de template com {Count} campos", dados.Count);

            try
            {
                // Criar cópia do template em memória para não modificar o original
                var outputStream = new MemoryStream();
                await templateStream.CopyToAsync(outputStream);
                outputStream.Position = 0;

                // Abrir documento Word
                using (var wordDocument = WordprocessingDocument.Open(outputStream, true))
                {
                    if (wordDocument.MainDocumentPart == null)
                    {
                        throw new InvalidOperationException("Template inválido: MainDocumentPart não encontrado");
                    }

                    var body = wordDocument.MainDocumentPart.Document.Body;

                    if (body == null)
                    {
                        throw new InvalidOperationException("Template inválido: Body do documento não encontrado");
                    }

                    // Normalizar texto antes da substituição (corrigir quebras de XML)
                    NormalizarTextoParaParagrafos(body);

                    // Substituir tags pelos valores
                    SubstituirTags(body, dados);

                    // Salvar alterações
                    wordDocument.MainDocumentPart.Document.Save();
                }

                // Resetar posição do stream para leitura
                outputStream.Position = 0;

                _logger.LogInformation("Template preenchido com sucesso");
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao preencher template");
                throw new InvalidOperationException("Erro ao processar template de documento", ex);
            }
        }

        /// <summary>
        /// Extrai todas as tags encontradas no template
        /// </summary>
        public async Task<List<string>> ExtrairTagsDoTemplateAsync(Stream templateStream)
        {
            _logger.LogInformation("Extraindo tags do template");

            try
            {
                var tags = new HashSet<string>();

                // Criar cópia para não modificar o stream original
                var tempStream = new MemoryStream();
                await templateStream.CopyToAsync(tempStream);
                tempStream.Position = 0;

                using (var wordDocument = WordprocessingDocument.Open(tempStream, false))
                {
                    if (wordDocument.MainDocumentPart?.Document.Body == null)
                    {
                        throw new InvalidOperationException("Template inválido");
                    }

                    var body = wordDocument.MainDocumentPart.Document.Body;

                    // Buscar em todos os textos do documento
                    foreach (var text in body.Descendants<Text>())
                    {
                        if (string.IsNullOrWhiteSpace(text.Text))
                            continue;

                        var matches = TagRegex.Matches(text.Text);
                        foreach (Match match in matches)
                        {
                            tags.Add(match.Value); // Adiciona a tag completa: <<NOME>>
                        }
                    }
                }

                _logger.LogInformation("Encontradas {Count} tags únicas no template", tags.Count);
                return tags.OrderBy(t => t).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao extrair tags do template");
                throw new InvalidOperationException("Erro ao analisar template", ex);
            }
        }

        /// <summary>
        /// Normaliza o texto dos parágrafos para corrigir quebras de XML causadas pelo Word
        /// Problema: O Word pode quebrar "<<NOME>>" em múltiplos elementos Run como:
        /// <Run>&lt;&lt;</Run><Run>NOME</Run><Run>&gt;&gt;</Run>
        /// Solução: Consolidar todos os Runs de um parágrafo em um único Run
        /// </summary>
        private void NormalizarTextoParaParagrafos(Body body)
        {
            var paragrafos = body.Descendants<Paragraph>().ToList();

            foreach (var paragrafo in paragrafos)
            {
                // Obter todos os Runs do parágrafo
                var runs = paragrafo.Descendants<Run>().ToList();

                if (runs.Count <= 1)
                    continue; // Já está normalizado

                // Concatenar texto de todos os Runs
                var textoCompleto = string.Join("", runs.Select(r =>
                {
                    var text = r.Descendants<Text>().FirstOrDefault();
                    return text?.Text ?? "";
                }));

                if (string.IsNullOrWhiteSpace(textoCompleto))
                    continue;

                // Preservar propriedades do primeiro Run
                var primeiroRun = runs.First();
                var runProperties = primeiroRun.RunProperties?.CloneNode(true) as RunProperties;

                // Remover todos os Runs existentes
                foreach (var run in runs)
                {
                    run.Remove();
                }

                // Criar novo Run consolidado
                var novoRun = new Run();

                if (runProperties != null)
                {
                    novoRun.AppendChild(runProperties);
                }

                var novoTexto = new Text(textoCompleto)
                {
                    Space = SpaceProcessingModeValues.Preserve // Preservar espaços
                };

                novoRun.AppendChild(novoTexto);
                paragrafo.AppendChild(novoRun);
            }
        }

        /// <summary>
        /// Substitui todas as tags encontradas pelos valores fornecidos
        /// </summary>
        private void SubstituirTags(Body body, Dictionary<string, string> dados)
        {
            var textos = body.Descendants<Text>().ToList();

            foreach (var text in textos)
            {
                if (string.IsNullOrWhiteSpace(text.Text))
                    continue;

                var textoOriginal = text.Text;
                var textoModificado = textoOriginal;

                // Substituir cada tag encontrada
                foreach (var kvp in dados)
                {
                    var tag = $"<<{kvp.Key}>>";
                    var valor = kvp.Value ?? string.Empty;

                    if (textoModificado.Contains(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        textoModificado = textoModificado.Replace(tag, valor, StringComparison.OrdinalIgnoreCase);

                        _logger.LogDebug(
                            "Tag {Tag} substituída por valor de {Length} caracteres",
                            tag,
                            valor.Length);
                    }
                }

                // Atualizar texto se houve modificação
                if (textoModificado != textoOriginal)
                {
                    text.Text = textoModificado;
                }
            }
        }

        /// <summary>
        /// Carrega um template do disco baseado no nome amigável configurado
        /// </summary>
        public async Task<Stream> CarregarTemplateAsync(string nomeTemplate)
        {
            if (!_settings.Templates.TryGetValue(nomeTemplate, out var nomeArquivo))
            {
                throw new FileNotFoundException($"Template '{nomeTemplate}' não configurado");
            }

            var caminhoCompleto = Path.Combine(_settings.CaminhoRaiz, nomeArquivo);

            if (!File.Exists(caminhoCompleto))
            {
                throw new FileNotFoundException($"Arquivo de template não encontrado: {caminhoCompleto}");
            }

            _logger.LogInformation("Carregando template: {Caminho}", caminhoCompleto);

            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(caminhoCompleto))
            {
                await fileStream.CopyToAsync(stream);
            }

            stream.Position = 0;
            return stream;
        }
    }
}
