"""Gera o projeto Power BI (PBIP) do SIG-Defesa Civil.

O .pbix é um container binário proprietário — não há como escrevê-lo fora do
Power BI Desktop. O PBIP é o formato de projeto equivalente, em texto, que a
Power BI Desktop abre nativamente e salva como .pbix.

Modelo: uma tabela-fato (Ocorrencias) e uma de tipificações, ambas com a coluna
ANO, mais medidas DAX. Assim todo indicador é fatiável por ano — o que tabelas
pré-agregadas não permitiriam.

Estruturas de tema e de visual copiadas de .pbix reais desta máquina, não
inferidas: ver as constantes TEMA / VERSAO_RELATORIO e os tipos em VISUAIS_OK.
"""
import hashlib, json, pathlib, shutil

RAIZ = pathlib.Path(r"C:\Users\lucio\Desktop\TCC")
NOME = "SIG-Defesa-Civil-BI"
MODELO = RAIZ / f"{NOME}.SemanticModel"
RELATORIO = RAIZ / f"{NOME}.Report"

SERVIDOR_DEV = "localhost:5432"
BANCO = "sig-defesa-civil"

TEMA = "CY26SU04"
VERSAO_RELATORIO = "5.72"
TEMAS_INSTALADOS = pathlib.Path(
    r"C:\Program Files\WindowsApps"
    r"\Microsoft.MicrosoftPowerBIDesktop_2.156.951.0_x64__8wekyb3d8bbwe"
    r"\bin\WebView2Resources\minerva\sharedresources\BaseThemes"
)

# Tipos de visual confirmados nos .pbix desta máquina. Não usar nada fora daqui.
VISUAIS_OK = {"card", "clusteredColumnChart", "slicer", "tableEx"}

TIPO = {
    "bigint": "int64", "integer": "int64",
    "numeric": "decimal", "double precision": "double",
    "text": "string", "boolean": "boolean", "date": "dateTime",
}

T = "\t"  # TMDL exige TAB

# ── Tabelas do modelo ───────────────────────────────────────────────────────
OCORRENCIAS = [
    ("ocorrencia_id", "integer"), ("protocolo", "text"),
    ("data_abertura", "date"), ("ano", "integer"), ("mes", "integer"),
    ("ano_mes_rotulo", "text"), ("periodo_chuvoso", "boolean"),
    ("status", "text"), ("status_ordem", "integer"), ("situacao", "text"),
    ("em_aberto", "boolean"), ("bairro", "text"),
    ("tem_avaliacao_risco", "boolean"), ("emergencia", "boolean"),
    ("tem_vistoria", "boolean"), ("data_vistoria", "date"),
    ("grau_risco_encontrado", "text"), ("tipo_risco", "text"),
    ("interdicao", "text"), ("remocao", "text"), ("regime_ocupacao", "text"),
    ("edificacao", "text"), ("estrutura", "text"),
    ("caracterizacao_local", "text"),
    ("risco_alto", "boolean"), ("houve_interdicao", "boolean"),
    ("total_moradores", "integer"), ("adultos", "integer"),
    ("criancas", "integer"), ("idosos", "integer"),
    ("pessoas_com_deficiencia", "integer"), ("pessoas_vulneraveis", "integer"),
    ("numero_moradias", "integer"),
    ("situacao_relatorio", "text"), ("tem_encaminhamento_final", "boolean"),
    ("qtd_notificados", "bigint"),
    ("dias_ate_vistoria", "integer"), ("faixa_tempo_vistoria", "text"),
    ("dias_em_aberto", "integer"), ("faixa_aging", "text"),
    ("dias_ate_encerramento", "integer"),
]

TIPIFICACOES = [
    ("ocorrencia_id", "integer"), ("protocolo", "text"), ("ano", "integer"),
    ("mes", "integer"), ("periodo_chuvoso", "boolean"), ("bairro", "text"),
    ("grau_risco_encontrado", "text"), ("risco_alto", "boolean"),
    ("houve_interdicao", "boolean"), ("em_aberto", "boolean"),
    ("tipificacao", "text"),
]

TABELAS = {
    "Ocorrencias": ("vw_bi_ocorrencias", OCORRENCIAS,
                    ("faixa_tempo_vistoria", None)),
    "Tipificacoes": ("vw_bi_tipificacao_ocorrencia", TIPIFICACOES, None),
}

# ── Medidas DAX ─────────────────────────────────────────────────────────────
# Uma linha cada: DAX multilinha em TMDL exige indentação de continuação e é
# fonte fácil de erro. Nenhuma medida referencia outra, pelo mesmo motivo.
INT = "#,0"
DEC = "#,0.0"
PCT = "0.0%"

MEDIDAS_OCORRENCIAS = [
    # Operação
    ("Ocorrências", "COUNTROWS(Ocorrencias)", INT),
    ("Em aberto", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[em_aberto] = TRUE())", INT),
    ("Encerradas", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[em_aberto] = FALSE())", INT),
    ("% encerradas", "DIVIDE(CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[em_aberto] = FALSE()), COUNTROWS(Ocorrencias))", PCT),
    ("Aguardando triagem", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[status] = \"ABERTA\")", INT),
    ("Aguardando vistoria", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[status] = \"VISTORIA_SOLICITADA\")", INT),
    ("Aguardando encerramento", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[status] = \"VISTORIA_REALIZADA\")", INT),
    ("Vistorias realizadas", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[tem_vistoria] = TRUE())", INT),
    ("Relatórios pendentes", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[situacao_relatorio] = \"Pendente\")", INT),
    ("Relatórios concluídos", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[situacao_relatorio] = \"Concluído\")", INT),
    # Tempo de resposta
    ("Média dias até vistoria", "AVERAGE(Ocorrencias[dias_ate_vistoria])", DEC),
    ("Mediana dias até vistoria", "MEDIAN(Ocorrencias[dias_ate_vistoria])", DEC),
    ("Atendidas em 7 dias", "DIVIDE(CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[dias_ate_vistoria] <= 7), COUNT(Ocorrencias[dias_ate_vistoria]))", PCT),
    ("Maior espera em aberto", "MAX(Ocorrencias[dias_em_aberto])", INT),
    ("Média dias até encerrar", "AVERAGE(Ocorrencias[dias_ate_encerramento])", DEC),
    # Características
    ("Risco alto", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[risco_alto] = TRUE())", INT),
    ("% risco alto", "DIVIDE(CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[risco_alto] = TRUE()), COUNTROWS(Ocorrencias))", PCT),
    ("Interdições", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[houve_interdicao] = TRUE())", INT),
    ("Emergências", "CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[emergencia] = TRUE())", INT),
    ("Bairros atendidos", "DISTINCTCOUNT(Ocorrencias[bairro])", INT),
    ("Período chuvoso", "DIVIDE(CALCULATE(COUNTROWS(Ocorrencias), Ocorrencias[periodo_chuvoso] = TRUE()), COUNTROWS(Ocorrencias))", PCT),
    # População exposta — zerada até a composição familiar ser preenchida.
    # Prefixo "Total de": medida e coluna não podem ter o mesmo nome na mesma
    # tabela (a comparação do Power BI ignora maiúsculas), e "Idosos" colidia
    # com a coluna "idosos".
    ("Total de moradores", "SUM(Ocorrencias[total_moradores])", INT),
    ("Total de crianças", "SUM(Ocorrencias[criancas])", INT),
    ("Total de idosos", "SUM(Ocorrencias[idosos])", INT),
    ("Total com deficiência", "SUM(Ocorrencias[pessoas_com_deficiencia])", INT),
    ("Total de vulneráveis", "SUM(Ocorrencias[pessoas_vulneraveis])", INT),
]

MEDIDAS_TIPIFICACOES = [
    ("Ocorrências tipificadas", "DISTINCTCOUNT(Tipificacoes[ocorrencia_id])", INT),
    ("Registros de tipificação", "COUNTROWS(Tipificacoes)", INT),
    ("Tipos distintos", "DISTINCTCOUNT(Tipificacoes[tipificacao])", INT),
    ("Tipificações de risco alto", "CALCULATE(COUNTROWS(Tipificacoes), Tipificacoes[risco_alto] = TRUE())", INT),
]


def _id(semente):
    return hashlib.md5(semente.encode()).hexdigest()[:20]


def escrever(caminho, conteudo):
    caminho.parent.mkdir(parents=True, exist_ok=True)
    caminho.write_text(conteudo, encoding="utf-8")


# ════════════════════════════════════════════════════════════════════════════
#  Modelo semântico (TMDL)
# ════════════════════════════════════════════════════════════════════════════

def tmdl_tabela(nome, view, colunas, ordenacao, medidas):
    l = [f"table {nome}", ""]

    for rotulo, dax, fmt in medidas:
        l.append(f"{T}measure '{rotulo}' = {dax}")
        l.append(f"{T*2}formatString: {fmt}")
        l.append(f"{T*2}lineageTag: {_id(nome + rotulo)[:8]}-0000-0000-0000-000000000000")
        l.append("")

    for col, pg in colunas:
        dt = TIPO[pg]
        l.append(f"{T}column {col}")
        l.append(f"{T*2}dataType: {dt}")
        if dt in ("int64", "decimal", "double"):
            l.append(f"{T*2}formatString: {INT if dt == 'int64' else DEC}")
        if dt == "dateTime":
            l.append(f"{T*2}formatString: yyyy-mm-dd")
        l.append(f"{T*2}summarizeBy: none")   # somas vêm das medidas, não das colunas
        l.append(f"{T*2}sourceColumn: {col}")
        if ordenacao and col == ordenacao[0] and ordenacao[1]:
            l.append(f"{T*2}sortByColumn: {ordenacao[1]}")
        l.append("")

    l += [
        f"{T}partition {nome} = m",
        f"{T*2}mode: import",
        f"{T*2}source =",
        f"{T*4}let",
        f"{T*5}Fonte = PostgreSQL.Database(Servidor, Banco),",
        f'{T*5}Dados = Fonte{{[Schema="public", Item="{view}"]}}[Data]',
        f"{T*4}in",
        f"{T*5}Dados",
        "",
        f"{T}annotation PBI_ResultType = Table",
        "",
    ]
    return "\n".join(l)


def gerar_modelo():
    escrever(MODELO / "definition.pbism", json.dumps({"version": "4.0", "settings": {}}, indent=2))
    escrever(MODELO / "definition" / "database.tmdl", f"database\n{T}compatibilityLevel: 1567\n")

    modelo = [
        "model Model",
        f"{T}culture: pt-BR",
        f"{T}defaultPowerBIDataSourceVersion: powerBI_V3",
        f"{T}sourceQueryCulture: pt-BR",
        "",
        f"{T}annotation PBI_QueryOrder = " + json.dumps(["Servidor", "Banco"] + list(TABELAS)),
        "",
    ]
    modelo += [f"ref table {t}" for t in TABELAS] + [""]
    escrever(MODELO / "definition" / "model.tmdl", "\n".join(modelo))

    escrever(MODELO / "definition" / "expressions.tmdl", "\n".join([
        f'expression Servidor = "{SERVIDOR_DEV}" meta '
        '[IsParameterQuery=true, Type="Text", IsParameterQueryRequired=true]',
        f"{T}lineageTag: 1a2b3c4d-0001-0000-0000-000000000001",
        "",
        f"{T}annotation PBI_ResultType = Text",
        "",
        f'expression Banco = "{BANCO}" meta '
        '[IsParameterQuery=true, Type="Text", IsParameterQueryRequired=true]',
        f"{T}lineageTag: 1a2b3c4d-0001-0000-0000-000000000002",
        "",
        f"{T}annotation PBI_ResultType = Text",
        "",
    ]))

    medidas = {"Ocorrencias": MEDIDAS_OCORRENCIAS, "Tipificacoes": MEDIDAS_TIPIFICACOES}
    for nome, (view, colunas, ordenacao) in TABELAS.items():
        escrever(MODELO / "definition" / "tables" / f"{nome}.tmdl",
                 tmdl_tabela(nome, view, colunas, ordenacao, medidas[nome]))

    escrever(MODELO / "diagramLayout.json", json.dumps({
        "version": "1.1.0", "diagrams": [{
            "ordinal": 0, "scrollPosition": {"x": 0, "y": 0},
            "nodes": [{"location": {"x": 40 + i * 280, "y": 40}, "nodeIndex": t,
                       "size": {"height": 240, "width": 240}, "zIndex": i}
                      for i, t in enumerate(TABELAS)],
            "name": "Todas as tabelas", "zoomValue": 100,
            "pinKeyFieldsToTop": False, "showExtraHeaderInfo": False,
            "hideKeyFieldsWhenCollapsed": False, "tablesLocked": False,
        }],
    }, indent=2, ensure_ascii=False))


# ════════════════════════════════════════════════════════════════════════════
#  Relatório
# ════════════════════════════════════════════════════════════════════════════

def _sel_coluna(fonte, tabela, coluna):
    return {"Column": {"Expression": {"SourceRef": {"Source": fonte}},
                       "Property": coluna},
            "Name": f"{tabela}.{coluna}"}


def _sel_medida(fonte, tabela, medida):
    return {"Measure": {"Expression": {"SourceRef": {"Source": fonte}},
                        "Property": medida},
            "Name": f"{tabela}.{medida}"}


def _container(semente, visual, x, y, w, h, z):
    return {
        "config": json.dumps({
            "name": _id(semente),
            "layouts": [{"id": 0, "position": {"x": x, "y": y, "z": z,
                                               "width": w, "height": h}}],
            "singleVisual": visual,
        }, ensure_ascii=False),
        "filters": "[]",
        "height": h, "width": w, "x": x, "y": y, "z": z,
    }


def cartao(tabela, medida, x, y, w=232, h=140, z=1000):
    f = tabela[0].lower()
    return _container(f"card{tabela}{medida}{x}{y}", {
        "visualType": "card",
        "projections": {"Values": [{"queryRef": f"{tabela}.{medida}"}]},
        "prototypeQuery": {"Version": 2,
                           "From": [{"Name": f, "Entity": tabela, "Type": 0}],
                           "Select": [_sel_medida(f, tabela, medida)]},
        "drillFilterOtherVisuals": True,
    }, x, y, w, h, z)


def colunas(tabela, categoria, medida, x, y, w, h, z=2000):
    f = tabela[0].lower()
    return _container(f"col{tabela}{categoria}{medida}{x}{y}", {
        "visualType": "clusteredColumnChart",
        "projections": {
            "Category": [{"queryRef": f"{tabela}.{categoria}"}],
            "Y": [{"queryRef": f"{tabela}.{medida}"}],
        },
        "prototypeQuery": {"Version": 2,
                           "From": [{"Name": f, "Entity": tabela, "Type": 0}],
                           "Select": [_sel_coluna(f, tabela, categoria),
                                      _sel_medida(f, tabela, medida)]},
        "drillFilterOtherVisuals": True,
    }, x, y, w, h, z)


def tabela_visual(tabela, campos, x, y, w, h, z=2000):
    """campos: lista de (nome, 'col'|'med')"""
    f = tabela[0].lower()
    select, refs = [], []
    for nome, tipo in campos:
        select.append(_sel_coluna(f, tabela, nome) if tipo == "col"
                      else _sel_medida(f, tabela, nome))
        refs.append({"queryRef": f"{tabela}.{nome}"})
    return _container(f"tab{tabela}{campos}{x}{y}", {
        "visualType": "tableEx",
        "projections": {"Values": refs},
        "prototypeQuery": {"Version": 2,
                           "From": [{"Name": f, "Entity": tabela, "Type": 0}],
                           "Select": select},
        "drillFilterOtherVisuals": True,
    }, x, y, w, h, z)


def filtro_ano(tabela, x=20, y=20, w=232, h=140, z=500):
    """Slicer de ano — é o corte que atravessa todos os indicadores."""
    f = tabela[0].lower()
    return _container(f"slicerAno{tabela}", {
        "visualType": "slicer",
        "projections": {"Values": [{"queryRef": f"{tabela}.ano", "active": True}]},
        "prototypeQuery": {"Version": 2,
                           "From": [{"Name": f, "Entity": tabela, "Type": 0}],
                           "Select": [_sel_coluna(f, tabela, "ano")]},
        "drillFilterOtherVisuals": True,
    }, x, y, w, h, z)


def pagina(titulo, visuais, ordem):
    return {
        "config": "{}",
        "displayName": titulo,
        "displayOption": 1,
        "filters": "[]",
        "height": 720,
        "width": 1280,
        "name": _id("pag" + titulo),
        "ordinal": ordem,
        "visualContainers": visuais,
    }


def montar_relatorio():
    O = "Ocorrencias"
    Tp = "Tipificacoes"

    # ── 1. Visão geral ──────────────────────────────────────────────────────
    p1 = [
        filtro_ano(O),
        cartao(O, "Ocorrências", 268, 20),
        cartao(O, "Em aberto", 516, 20),
        cartao(O, "Risco alto", 764, 20),
        cartao(O, "Interdições", 1012, 20),
        colunas(O, "ano", "Ocorrências", 20, 180, 380, 250),
        colunas(O, "ano_mes_rotulo", "Ocorrências", 416, 180, 844, 250),
        colunas(O, "status", "Ocorrências", 20, 448, 620, 250, 3000),
        colunas(O, "faixa_tempo_vistoria", "Ocorrências", 656, 448, 604, 250, 3000),
    ]

    # ── 2. Operação ─────────────────────────────────────────────────────────
    p2 = [
        filtro_ano(O),
        cartao(O, "Aguardando vistoria", 268, 20),
        cartao(O, "Aguardando encerramento", 516, 20),
        cartao(O, "Relatórios pendentes", 764, 20),
        cartao(O, "Mediana dias até vistoria", 1012, 20),
        cartao(O, "Atendidas em 7 dias", 20, 180, 232, 130),
        cartao(O, "Média dias até vistoria", 268, 180, 232, 130),
        cartao(O, "Maior espera em aberto", 516, 180, 232, 130),
        cartao(O, "% encerradas", 764, 180, 232, 130),
        cartao(O, "Vistorias realizadas", 1012, 180, 232, 130),
        colunas(O, "faixa_aging", "Em aberto", 20, 330, 620, 250, 3000),
        colunas(O, "situacao_relatorio", "Ocorrências", 656, 330, 604, 250, 3000),
        tabela_visual(O, [("ano", "col"), ("Ocorrências", "med"),
                          ("Vistorias realizadas", "med"),
                          ("Mediana dias até vistoria", "med"),
                          ("Atendidas em 7 dias", "med"),
                          ("Relatórios pendentes", "med")],
                      20, 596, 1240, 110, 4000),
    ]

    # ── 3. Características ──────────────────────────────────────────────────
    p3 = [
        filtro_ano(O),
        cartao(O, "% risco alto", 268, 20),
        cartao(O, "Interdições", 516, 20),
        cartao(O, "Bairros atendidos", 764, 20),
        cartao(O, "Período chuvoso", 1012, 20),
        colunas(O, "grau_risco_encontrado", "Ocorrências", 20, 180, 400, 250),
        colunas(O, "interdicao", "Ocorrências", 436, 180, 400, 250),
        colunas(O, "mes", "Ocorrências", 852, 180, 408, 250),
        tabela_visual(O, [("bairro", "col"), ("Ocorrências", "med"),
                          ("Risco alto", "med"), ("% risco alto", "med"),
                          ("Interdições", "med"), ("Em aberto", "med")],
                      20, 448, 620, 258, 4000),
        tabela_visual(O, [("ano", "col"), ("Total de moradores", "med"),
                          ("Total de crianças", "med"), ("Total de idosos", "med"),
                          ("Total com deficiência", "med")],
                      656, 448, 604, 258, 4000),
    ]

    # ── 4. Tipos de risco ───────────────────────────────────────────────────
    p4 = [
        filtro_ano(Tp),
        cartao(Tp, "Tipos distintos", 268, 20),
        cartao(Tp, "Ocorrências tipificadas", 516, 20),
        cartao(Tp, "Registros de tipificação", 764, 20),
        cartao(Tp, "Tipificações de risco alto", 1012, 20),
        tabela_visual(Tp, [("tipificacao", "col"), ("Ocorrências tipificadas", "med"),
                           ("Tipificações de risco alto", "med")],
                      20, 180, 620, 520, 4000),
        colunas(Tp, "grau_risco_encontrado", "Ocorrências tipificadas",
                656, 180, 604, 250),
        tabela_visual(Tp, [("bairro", "col"), ("Ocorrências tipificadas", "med"),
                           ("Tipificações de risco alto", "med")],
                      656, 448, 604, 252, 4000),
    ]

    return {
        "config": json.dumps({
            "version": VERSAO_RELATORIO,
            "themeCollection": {"baseTheme": {
                "name": TEMA, "type": 2,
                "version": {"visual": "2.8.0", "report": "3.2.0", "page": "2.3.1"}}},
            "activeSectionIndex": 0,
            "defaultDrillFilterOtherVisuals": True,
            "settings": {
                "useNewFilterPaneExperience": True,
                "allowChangeFilterTypes": True,
                "useStylableVisualContainerHeader": True,
                "useEnhancedTooltips": True,
            },
        }, ensure_ascii=False),
        "layoutOptimization": 0,
        "resourcePackages": [{"resourcePackage": {
            "name": "SharedResources", "type": 2,
            "items": [{"type": 202, "path": f"BaseThemes/{TEMA}.json", "name": TEMA}],
            "disabled": False}}],
        "sections": [
            pagina("Visão geral", p1, 0),
            pagina("Operação", p2, 1),
            pagina("Características", p3, 2),
            pagina("Tipos de risco", p4, 3),
        ],
    }


def conferir_nomes():
    """Medida e coluna não podem dividir o mesmo nome dentro de uma tabela.
    O Power BI compara ignorando maiúsculas; aqui também ignoramos acento, para
    não depender do collation. Falha cedo, antes de gerar o projeto."""
    import unicodedata

    def chave(s):
        s = unicodedata.normalize("NFKD", s).encode("ascii", "ignore").decode()
        return s.casefold().strip()

    medidas = {"Ocorrencias": MEDIDAS_OCORRENCIAS, "Tipificacoes": MEDIDAS_TIPIFICACOES}
    problemas = []
    for tabela, (_, colunas, _) in TABELAS.items():
        nomes_col = {chave(c) for c, _ in colunas}
        vistos = set()
        for rotulo, _, _ in medidas[tabela]:
            k = chave(rotulo)
            if k in nomes_col:
                problemas.append(f"{tabela}: medida '{rotulo}' colide com uma coluna")
            if k in vistos:
                problemas.append(f"{tabela}: medida '{rotulo}' duplicada")
            vistos.add(k)
    if problemas:
        raise SystemExit("Conflito de nomes no modelo:\n  " + "\n  ".join(problemas))


def main():
    conferir_nomes()

    for pasta in (MODELO, RELATORIO):
        if pasta.exists():
            shutil.rmtree(pasta)

    escrever(RAIZ / f"{NOME}.pbip", json.dumps({
        "version": "1.0",
        "artifacts": [{"report": {"path": f"{NOME}.Report"}}],
        "settings": {"enableAutoRecovery": True},
    }, indent=2, ensure_ascii=False))

    gerar_modelo()

    escrever(RELATORIO / "definition.pbir", json.dumps({
        "version": "4.0",
        "datasetReference": {"byPath": {"path": f"../{NOME}.SemanticModel"}},
    }, indent=2))

    destino_tema = RELATORIO / "StaticResources" / "SharedResources" / "BaseThemes" / f"{TEMA}.json"
    destino_tema.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(TEMAS_INSTALADOS / f"{TEMA}.json", destino_tema)

    escrever(RELATORIO / "report.json",
             json.dumps(montar_relatorio(), indent=2, ensure_ascii=False))

    escrever(RAIZ / f"{NOME}.pbids", json.dumps({
        "version": "0.1",
        "connections": [{"details": {"protocol": "postgresql",
                                     "address": {"server": SERVIDOR_DEV, "database": BANCO}},
                         "mode": "Import"}],
    }, indent=2))

    rep = montar_relatorio()
    print("Gerado em", RAIZ)
    print(f"  {len(TABELAS)} tabelas | "
          f"{len(MEDIDAS_OCORRENCIAS) + len(MEDIDAS_TIPIFICACOES)} medidas | "
          f"{len(rep['sections'])} páginas | "
          f"{sum(len(s['visualContainers']) for s in rep['sections'])} visuais")


if __name__ == "__main__":
    main()
