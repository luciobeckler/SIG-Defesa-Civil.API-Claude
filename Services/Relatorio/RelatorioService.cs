using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIG_Defesa_Civil.API.Data.Configuration.DocumentTemplate;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.Tabelas;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Services.Storage;
using EncaminhamentoEnum = SIG_Defesa_Civil.API.Enums.Encaminhamento;

namespace SIG_Defesa_Civil.API.Services.Relatorio
{
    public class RelatorioService : IRelatorioService
    {
        private readonly DefesaCivilContext _context;
        private readonly IStorageService _storageService;
        private readonly TemplateSettings _templateSettings;
        private readonly ILogger<RelatorioService> _logger;

        // Nome determinístico: uma ocorrência → um relatório final
        private static string NomeRelatorio(int ocorrenciaId) =>
            $"relatorio_final_{ocorrenciaId}.docx";

        public RelatorioService(
            DefesaCivilContext context,
            IStorageService storageService,
            IOptions<TemplateSettings> templateSettings,
            ILogger<RelatorioService> logger)
        {
            _context = context;
            _storageService = storageService;
            _templateSettings = templateSettings.Value;
            _logger = logger;
        }

        // ── Geração ──────────────────────────────────────────────────────────────────

        public async Task<string> GerarRelatorioAsync(int ocorrenciaId, int vistoriaId, int usuarioId)
        {
            var ocorrencia = await _context.Ocorrencias
                .Include(o => o.Solicitante)
                .Include(o => o.Localizacao)
                .Include(o => o.AvaliacaoRisco)
                .Include(o => o.Agendamentos).ThenInclude(a => a.Vistoriador1)
                .Include(o => o.Agendamentos).ThenInclude(a => a.Vistoriador2)
                .Include(o => o.Vistorias)
                .Include(o => o.Notificados)
                .Include(o => o.EncaminhamentoFinal)
                .Include(o => o.Arquivos)
                .Where(o => o.DeletedAt == null)
                .FirstOrDefaultAsync(o => o.Id == ocorrenciaId)
                ?? throw new InvalidOperationException($"Ocorrência {ocorrenciaId} não encontrada.");

            var vistoria = ocorrencia.Vistorias.FirstOrDefault(v => v.Id == vistoriaId)
                ?? throw new InvalidOperationException(
                    $"Vistoria {vistoriaId} não encontrada para a ocorrência {ocorrenciaId}.");

            // Agendamento vinculado (para obter vistoriadores e matrículas)
            var agendamento = vistoria.AgendamentoId.HasValue
                ? ocorrencia.Agendamentos.FirstOrDefault(a => a.Id == vistoria.AgendamentoId.Value)
                : ocorrencia.Agendamentos.OrderByDescending(a => a.Numero).FirstOrDefault();

            var loc  = ocorrencia.Localizacao;
            var sol  = ocorrencia.Solicitante;
            var aval = ocorrencia.AvaliacaoRisco;
            var notificados = ocorrencia.Notificados.OrderBy(n => n.RegistradoEm).ToList();

            var encaminhamentosStr = vistoria.EncaminhamentosDeCampo.Count > 0
                ? string.Join(", ", vistoria.EncaminhamentosDeCampo.Select(FormatarEncaminhamento))
                : ocorrencia.EncaminhamentoFinal?.Encaminhamentos.Count > 0
                    ? string.Join(", ", ocorrencia.EncaminhamentoFinal.Encaminhamentos.Select(e => FormatarEncaminhamento(e.ToString())))
                    : string.Empty;

            var tags = new Dictionary<string, string>
            {
                ["<<PROTOCOLO>>"]                  = ocorrencia.Protocolo,
                ["<<GRAU_RISCO_INICIAL>>"]         = aval?.GrauRiscoInicial.ToString() ?? string.Empty,
                ["<<REQUISICAO_SETOR_DOCUMENTO>>"] = aval?.RequisicaoSetorDocumento ?? string.Empty,
                ["<<DATA_SOLICITACAO>>"]            = ocorrencia.AbertaEm.ToLocalTime().ToString("dd/MM/yyyy"),
                ["<<HORARIO_SOLICITACAO>>"]         = ocorrencia.AbertaEm.ToLocalTime().ToString("HH:mm"),
                ["<<NOME>>"]                        = sol.Nome,
                ["<<CPF>>"]                         = sol.Cpf ?? string.Empty,
                ["<<IDENTIDADE>>"]                  = sol.Rg ?? string.Empty,
                ["<<ORGAO_EMISSOR>>"]               = sol.OrgaoEmissor ?? string.Empty,
                ["<<ENDERECO>>"]                    = loc?.Endereco ?? string.Empty,
                ["<<NUMERO>>"]                      = loc?.Numero ?? string.Empty,
                ["<<COMPLEMENTO>>"]                 = loc?.Complemento ?? string.Empty,
                ["<<CEP>>"]                         = loc?.Cep ?? string.Empty,
                ["<<BAIRRO>>"]                      = loc?.Bairro ?? string.Empty,
                ["<<CIDADE>>"]                      = loc?.Cidade ?? string.Empty,
                ["<<UF>>"]                          = loc?.Uf ?? string.Empty,
                ["<<TELEFONE>>"]                    = sol.Telefone ?? string.Empty,
                ["<<CELULAR>>"]                     = sol.Celular ?? string.Empty,
                ["<<EMAIL>>"]                       = sol.Email,
                ["<<DATA_VISTORIA>>"]               = vistoria.DataVistoria.ToString("dd/MM/yyyy"),
                ["<<HORARIO_INICIO_VISTORIA>>"]     = vistoria.HorarioInicio.ToString(@"hh\:mm"),
                ["<<NOME_VISTORIADOR_1>>"]          = agendamento?.Vistoriador1?.Nome ?? string.Empty,
                ["<<NOME_VISTORIADOR_2>>"]          = agendamento?.Vistoriador2?.Nome ?? string.Empty,
                ["<<DIA>>"]                         = vistoria.DataVistoria.Day.ToString("D2"),
                ["<<MES>>"]                         = vistoria.DataVistoria.Month.ToString("D2"),
                ["<<MATRICULA_VISTORIADOR_1>>"]     = agendamento?.Vistoriador1?.Matricula ?? string.Empty,
                ["<<MATRICULA_VISTORIADOR_2>>"]     = agendamento?.Vistoriador2?.Matricula ?? string.Empty,
                ["<<ENCAMINHAMENTOS>>"]             = encaminhamentosStr,
            };

            // Notificados dinâmicos — suporta até 5 entradas com tags <<NOME_NOTIFICADO_N>>
            // Para cadastrar mais que 2 no template, basta adicionar as linhas com
            // <<NOME_NOTIFICADO_3>>, <<RG_CPF_NOTIFICADO_3>>, <<DATA_NOTIFICADO_3>>, etc.
            const int maxNotificados = 5;
            for (int i = 0; i < maxNotificados; i++)
            {
                var n   = notificados.ElementAtOrDefault(i);
                var idx = i + 1;
                tags[$"<<NOME_NOTIFICADO_{idx}>>"]    = n?.Nome ?? string.Empty;
                tags[$"<<RG_CPF_NOTIFICADO_{idx}>>"]  = n?.RgCpf ?? string.Empty;
                tags[$"<<DATA_NOTIFICADO_{idx}>>"]    = n?.DataNotificacao.ToString("dd/MM/yyyy") ?? string.Empty;
            }

            if (!_templateSettings.Templates.TryGetValue("RelatorioVistoria", out var templateFileName))
                throw new InvalidOperationException("Template 'RelatorioVistoria' não configurado em TemplateSettings.");

            var templatePath = Path.Combine(_templateSettings.CaminhoRaiz, templateFileName);
            if (!File.Exists(templatePath))
                throw new InvalidOperationException($"Arquivo de template não encontrado: {templatePath}");

            // Copia para MemoryStream redimensionável — OpenXml precisa escrever no stream
            byte[] templateBytes = await File.ReadAllBytesAsync(templatePath);
            var ms = new MemoryStream();
            await ms.WriteAsync(templateBytes);
            ms.Position = 0;

            using (var wordDoc = WordprocessingDocument.Open(ms, true))
            {
                SubstituirTags(wordDoc, tags);
                wordDoc.Save();
            }

            ms.Position = 0;

            var nomeArquivo = NomeRelatorio(ocorrenciaId);
            await _storageService.CriarEstruturaPastasAsync(ocorrencia.Protocolo);
            var caminho = await _storageService.SalvarArquivoAsync(
                ocorrencia.Protocolo, nomeArquivo, TipoArquivo.RELATORIO_FINAL, ms);

            // Remove registro anterior se existir (substituição)
            var anterior = await _context.Arquivos.FirstOrDefaultAsync(a =>
                a.OcorrenciaId == ocorrenciaId &&
                a.TipoArquivo  == TipoArquivo.RELATORIO_FINAL.ToString() &&
                a.NomeOriginal == nomeArquivo);

            if (anterior != null)
                _context.Arquivos.Remove(anterior);

            _context.Arquivos.Add(new Arquivo
            {
                OcorrenciaId     = ocorrenciaId,
                NomeOriginal     = nomeArquivo,
                TipoArquivo      = TipoArquivo.RELATORIO_FINAL.ToString(),
                CaminhoRelativo  = caminho,
                TamanhoBytes     = ms.Length,
                EnviadoPorUserId = usuarioId,
                EnviadoEm        = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Relatório final gerado para ocorrência {Protocolo} (vistoria {VistoriaId}) pelo usuário {UsuarioId}",
                ocorrencia.Protocolo, vistoriaId, usuarioId);

            return caminho;
        }

        // ── Exclusão ─────────────────────────────────────────────────────────────────

        public async Task ExcluirRelatorioAsync(int ocorrenciaId)
        {
            var nomeArquivo = NomeRelatorio(ocorrenciaId);

            var arquivo = await _context.Arquivos.FirstOrDefaultAsync(a =>
                a.OcorrenciaId == ocorrenciaId &&
                a.TipoArquivo  == TipoArquivo.RELATORIO_FINAL.ToString() &&
                a.NomeOriginal == nomeArquivo)
                ?? throw new InvalidOperationException(
                    $"Nenhum relatório final encontrado para a ocorrência {ocorrenciaId}.");

            _context.Arquivos.Remove(arquivo);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Relatório final removido (registro DB) para ocorrência {OcorrenciaId}", ocorrenciaId);
        }

        // ── Substituição de tags via normalização de runs ────────────────────────────

        private static void SubstituirTags(WordprocessingDocument doc, Dictionary<string, string> tags)
        {
            var body = doc.MainDocumentPart!.Document.Body!;

            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var runs = paragraph.Elements<Run>().ToList();
                if (runs.Count == 0) continue;

                var fullText = string.Concat(
                    runs.SelectMany(r => r.Elements<Text>()).Select(t => t.Text));

                if (!fullText.Contains("<<")) continue;

                foreach (var (tag, value) in tags)
                    fullText = fullText.Replace(tag, value);

                // Mantém primeiro run (preserva formatação), remove os demais
                var firstRun = runs[0];
                for (int i = 1; i < runs.Count; i++)
                    runs[i].Remove();

                foreach (var t in firstRun.Elements<Text>().ToList())
                    t.Remove();

                firstRun.AppendChild(new Text(fullText));
            }
        }

        // ── Formatação de enums ───────────────────────────────────────────────────────

        // Aceita tanto os valores do enum (ex.: "SEC_OBRAS") quanto opções
        // personalizadas (texto livre), que são exibidas como foram digitadas.
        private static string FormatarEncaminhamento(string valor) => valor switch
        {
            "AJUDA_HUMANITARIA"         => "Ajuda Humanitária",
            "CEMIG"                     => "CEMIG",
            "COPASA"                    => "COPASA",
            "DNIT"                      => "DNIT",
            "OUTROS"                    => "Outros",
            "PROVIDENCIAS_PELO_MORADOR" => "Providências pelo Morador",
            "SEC_DESENV_SOCIAL"         => "Sec. Desenvolvimento Social",
            "SEC_MEIO_AMBIENTE"         => "Sec. Meio Ambiente",
            "SEC_OBRAS"                 => "Sec. Obras",
            _                           => valor
        };
    }
}
