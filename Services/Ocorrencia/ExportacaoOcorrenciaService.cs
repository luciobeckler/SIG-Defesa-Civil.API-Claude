using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Models;

namespace SIG_Defesa_Civil.API.Services.Ocorrencia
{
    /// <summary>
    /// Exporta a listagem de ocorrências para .xlsx, respeitando o filtro da tela.
    /// </summary>
    /// <remarks>
    /// <b>Sem dados do solicitante</b>, por decisão da coordenação: nome, CPF, e-mail
    /// e telefone ficam de fora. A planilha sai do sistema e deixa de ter controle de
    /// acesso, então carrega só o que serve para estatística e acompanhamento —
    /// protocolo, endereço, risco e andamento.
    ///
    /// Uma linha por ocorrência. Quando há mais de uma vistoria ou agendamento, vale
    /// o mais recente: a planilha é um retrato do estado atual, não um histórico.
    /// </remarks>
    public interface IExportacaoOcorrenciaService
    {
        Task<byte[]> ExportarAsync(FiltroOcorrenciaDto filtros);
    }

    public class ExportacaoOcorrenciaService : IExportacaoOcorrenciaService
    {
        private readonly DefesaCivilContext _context;
        private readonly IOcorrenciaService _ocorrenciaService;
        private readonly ILogger<ExportacaoOcorrenciaService> _logger;

        /// <summary>Limite de segurança: a base inteira num .xlsx trava o Excel e o servidor.</summary>
        private const int MAX_LINHAS = 20_000;

        public ExportacaoOcorrenciaService(
            DefesaCivilContext context,
            IOcorrenciaService ocorrenciaService,
            ILogger<ExportacaoOcorrenciaService> logger)
        {
            _context = context;
            _ocorrenciaService = ocorrenciaService;
            _logger = logger;
        }

        public async Task<byte[]> ExportarAsync(FiltroOcorrenciaDto filtros)
        {
            var ocorrencias = await _ocorrenciaService
                .ConsultaFiltrada(filtros)
                .Include(o => o.Localizacao)
                .Include(o => o.AvaliacaoRisco)
                .Include(o => o.Agendamentos).ThenInclude(a => a.Vistoriador1)
                .Include(o => o.Agendamentos).ThenInclude(a => a.Vistoriador2)
                .Include(o => o.Vistorias)
                .Include(o => o.EncaminhamentoFinal)
                .OrderByDescending(o => o.AbertaEm)
                .Take(MAX_LINHAS)
                .AsSplitQuery()
                .ToListAsync();

            using var planilha = new XLWorkbook();
            var aba = planilha.Worksheets.Add("Ocorrências");

            string[] cabecalhos =
            {
                "Protocolo", "Status", "Aberta em", "Encerrada em",
                "Bairro", "Endereço", "Número", "Complemento", "Cidade", "UF",
                "Ponto de referência", "Coordenada", "IPTU",
                "Descrição do problema",
                "Grau de risco inicial", "Emergência", "Tipificação inicial",
                "Data agendada", "Turno", "Equipe",
                "Data da vistoria", "Grau de risco encontrado", "Tipificação da vistoria",
                "Caracterização do local", "Regime de ocupação",
                "Interdição", "Remoção", "Total de moradores",
                "Encaminhamento final",
            };
            for (int c = 0; c < cabecalhos.Length; c++)
                aba.Cell(1, c + 1).Value = cabecalhos[c];

            var linha = 2;
            foreach (var o in ocorrencias)
            {
                var loc = o.Localizacao;
                var aval = o.AvaliacaoRisco;
                // O mais recente manda: a planilha retrata o estado de hoje.
                var ag = o.Agendamentos.OrderByDescending(a => a.Numero).FirstOrDefault();
                var v = o.Vistorias.OrderByDescending(x => x.Numero).FirstOrDefault();

                var equipe = ag == null
                    ? string.Empty
                    : string.Join("; ", new[] { ag.Vistoriador1?.Nome, ag.Vistoriador2?.Nome }
                        .Where(n => !string.IsNullOrWhiteSpace(n)));

                var col = 1;
                aba.Cell(linha, col++).Value = o.Protocolo;
                aba.Cell(linha, col++).Value = o.Status.ToString();
                aba.Cell(linha, col++).Value = o.AbertaEm.ToLocalTime();
                aba.Cell(linha, col++).Value = o.EncaminhamentoFinal?.RegistradoEm.ToLocalTime();

                aba.Cell(linha, col++).Value = loc?.Bairro;
                aba.Cell(linha, col++).Value = loc?.Endereco;
                aba.Cell(linha, col++).Value = loc?.Numero;
                aba.Cell(linha, col++).Value = loc?.Complemento;
                aba.Cell(linha, col++).Value = loc?.Cidade;
                aba.Cell(linha, col++).Value = loc?.Uf;
                aba.Cell(linha, col++).Value = loc?.Referencia;
                aba.Cell(linha, col++).Value = loc?.Coordenada;
                aba.Cell(linha, col++).Value = loc?.NumeroIptu;

                aba.Cell(linha, col++).Value = o.DescricaoProblema;

                aba.Cell(linha, col++).Value = aval?.GrauRiscoInicial.ToString();
                aba.Cell(linha, col++).Value = aval == null ? null : (aval.Emergencia ? "Sim" : "Não");
                aba.Cell(linha, col++).Value = aval == null
                    ? null : string.Join("; ", aval.TipificacaoInicial);

                aba.Cell(linha, col++).Value = ag?.Data?.ToDateTime(TimeOnly.MinValue);
                aba.Cell(linha, col++).Value = ag?.Turno?.ToString();
                aba.Cell(linha, col++).Value = equipe;

                aba.Cell(linha, col++).Value = v?.DataVistoria.ToDateTime(TimeOnly.MinValue);
                aba.Cell(linha, col++).Value = v?.GrauRiscoEncontrado;
                aba.Cell(linha, col++).Value = v == null
                    ? null : string.Join("; ", v.TipificacaoOcorrencia);
                aba.Cell(linha, col++).Value = v == null
                    ? null : string.Join("; ", v.CaracterizacaoDoLocal);
                aba.Cell(linha, col++).Value = v == null
                    ? null : string.Join("; ", v.RegimeOcupacao);
                aba.Cell(linha, col++).Value = v?.Interdicao;
                aba.Cell(linha, col++).Value = v?.Remocao;
                aba.Cell(linha, col++).Value = v?.TotalMoradores;

                aba.Cell(linha, col).Value = o.EncaminhamentoFinal == null
                    ? null
                    : string.Join("; ", o.EncaminhamentoFinal.Encaminhamentos.Select(e => e.ToString()));

                linha++;
            }

            // Formatação depois dos dados: aplicar por coluna de uma vez é bem mais
            // rápido do que célula a célula.
            var cabecalho = aba.Range(1, 1, 1, cabecalhos.Length);
            cabecalho.Style.Font.Bold = true;
            cabecalho.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2127");
            cabecalho.Style.Font.FontColor = XLColor.White;

            if (linha > 2)
            {
                aba.Range(2, 3, linha - 1, 4).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
                aba.Range(2, 18, linha - 1, 18).Style.DateFormat.Format = "dd/mm/yyyy";
                aba.Range(2, 21, linha - 1, 21).Style.DateFormat.Format = "dd/mm/yyyy";
                aba.Range(1, 1, linha - 1, cabecalhos.Length).SetAutoFilter();
            }

            aba.SheetView.FreezeRows(1);
            aba.Columns().AdjustToContents(1, 200, 8, 55);

            _logger.LogInformation("Exportação gerada com {Linhas} ocorrências", linha - 2);

            using var memoria = new MemoryStream();
            planilha.SaveAs(memoria);
            return memoria.ToArray();
        }
    }
}
