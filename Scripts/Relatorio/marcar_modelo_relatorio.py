"""Insere os marcadores <<TAG>> no modelo oficial do relatório.

    python marcar_modelo_relatorio.py <modelo-em-branco.docx> <saida.docx>

O modelo que a Defesa Civil entrega vem **em branco**: cada campo é um rótulo
seguido do espaço onde se digita ("NOME:", "CPF:"). O gerador do sistema troca
marcadores `<<TAG>>`, então este script anexa o marcador logo depois de cada
rótulo, dentro da mesma célula — é assim que o documento é montado, rótulo e
valor convivem no mesmo parágrafo.

Rodar de novo num arquivo já marcado não duplica nada: o script pula o rótulo
que já tiver marcador à frente.
"""
import html
import re
import shutil
import sys
import zipfile

# (rótulo exatamente como aparece no documento, marcador a anexar)
#
# A ordem importa: rótulos mais longos primeiro, senão "DATA:" casaria dentro de
# outro rótulo que o contenha.
MARCADORES = [
    # ── Dados da solicitação ────────────────────────────────────────────────
    ("REQUISIÇÃO (SETOR/DOCUMENTO):", "<<REQUISICAO_SETOR_DOCUMENTO>>"),
    ("PROTOCOLO Nº 2026", "<<PROTOCOLO_SEQ>>"),
    ("Nº DE CHAMADO: 2026", "<<PROTOCOLO_SEQ>>"),

    # ── Solicitante ─────────────────────────────────────────────────────────
    ("TELEFONE RESIDENCIAL:", "<<TELEFONE>>"),
    ("TELEFONE CELULAR:", "<<CELULAR>>"),
    ("ÓRGÃO EMISSOR:", "<<ORGAO_EMISSOR>>"),
    ("COMPLEMENTO:", "<<COMPLEMENTO>>"),
    ("IDENTIDADE:", "<<IDENTIDADE>>"),
    ("ENDEREÇO:", "<<ENDERECO>>"),
    ("BAIRRO:", "<<BAIRRO>>"),
    ("EMAIL:", "<<EMAIL>>"),
    ("NOME:", "<<NOME>>"),
    ("CPF:", "<<CPF>>"),
    ("CEP:", "<<CEP>>"),
    ("Nº.:", "<<NUMERO>>"),

    # ── Vistoria ────────────────────────────────────────────────────────────
    ("QUADRO/RISCO ENCONTRADO NO LOCAL:", "<<GRAU_RISCO_ENCONTRADO>>"),
    ("AGENTE(S):", "<<AGENTES>>"),

    # ── Equipe ──────────────────────────────────────────────────────────────
    ("VISTORIADOR 01:", "<<NOME_VISTORIADOR_1>>"),
    ("VISTORIADOR 02:", "<<NOME_VISTORIADOR_2>>"),
    ("VISTORIADOR 03:", "<<NOME_VISTORIADOR_3>>"),

    # ── Notificados ─────────────────────────────────────────────────────────
    ("NOTIFICADO 01:", "<<NOME_NOTIFICADO_1>>"),
    ("NOTIFICADO 02:", "<<NOME_NOTIFICADO_2>>"),
]

# Rótulos que se repetem e precisam de marcador diferente a cada ocorrência,
# na ordem em que aparecem no documento.
SEQUENCIAIS = {
    # Primeiro par DATA/HORA = abertura da ocorrência; o segundo = atendimento.
    "DATA:": ["<<DATA_SOLICITACAO>>", "<<DATA_VISTORIA>>"],
    "HORA:": ["<<HORARIO_SOLICITACAO>>", "<<HORARIO_INICIO_VISTORIA>>"],
    # Três matrículas de vistoriador, depois das duas fixas da diretoria.
    "MATRÍCULA:": ["", "", "<<MATRICULA_VISTORIADOR_1>>",
                   "<<MATRICULA_VISTORIADOR_2>>", "<<MATRICULA_VISTORIADOR_3>>"],
    "RG/CPF:": ["<<RG_CPF_NOTIFICADO_1>>", "<<RG_CPF_NOTIFICADO_2>>"],
}


def texto_visivel(fragmento: str) -> str:
    return html.unescape(re.sub(r"<[^>]+>", "", fragmento))


def marcar(xml: str) -> tuple[str, int]:
    """Anexa cada marcador ao fim do <w:t> que fecha o rótulo."""
    total = 0
    contadores = {k: 0 for k in SEQUENCIAIS}

    # Percorre os <w:t> na ordem do documento. O rótulo pode estar dividido em
    # vários runs, então a comparação é feita sobre o texto acumulado do
    # parágrafo, e o marcador entra no último <w:t> dele.
    def processar_paragrafo(m):
        nonlocal total
        par = m.group(0)
        visivel = texto_visivel(par).strip()
        if not visivel or "<<" in visivel:
            return par  # vazio, ou já marcado

        for rotulo, marcador in MARCADORES:
            if visivel.startswith(rotulo) or visivel == rotulo:
                return inserir(par, marcador) if marcador else par

        for rotulo, lista in SEQUENCIAIS.items():
            if visivel.startswith(rotulo) or visivel == rotulo:
                i = contadores[rotulo]
                contadores[rotulo] += 1
                marcador = lista[i] if i < len(lista) else ""
                return inserir(par, marcador) if marcador else par
        return par

    def inserir(par, marcador):
        nonlocal total
        # Depois do conteúdo do último <w:t> do parágrafo.
        ts = list(re.finditer(r"(<w:t(?: [^>]*)?>)(.*?)(</w:t>)", par, re.S))
        if not ts:
            return par
        ultimo = ts[-1]
        total += 1
        # Escapado: '<' cru dentro de <w:t> quebra o XML e o Word recusa o arquivo.
        # Os modelos antigos guardam os marcadores assim, como &lt;&lt;TAG&gt;&gt;.
        escapado = marcador.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
        return (par[:ultimo.start()]
                + ultimo.group(1) + ultimo.group(2) + " " + escapado + ultimo.group(3)
                + par[ultimo.end():])

    xml = re.sub(r"<w:p(?: [^>]*)?>.*?</w:p>", processar_paragrafo, xml, flags=re.S)
    return xml, total


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 1
    origem, destino = sys.argv[1], sys.argv[2]
    shutil.copyfile(origem, destino)

    with zipfile.ZipFile(origem) as z:
        partes = {n: z.read(n) for n in z.namelist()}

    xml = partes["word/document.xml"].decode("utf-8")
    xml, total = marcar(xml)
    partes["word/document.xml"] = xml.encode("utf-8")

    with zipfile.ZipFile(destino, "w", zipfile.ZIP_DEFLATED) as z:
        for nome, conteudo in partes.items():
            z.writestr(nome, conteudo)

    # Desescapa primeiro: no XML os marcadores vivem como &lt;&lt;TAG&gt;&gt;.
    achados = sorted(set(re.findall(r"<<[A-Z0-9_]+>>", html.unescape(xml))))
    print(f"{total} marcadores inseridos; {len(achados)} distintos:")
    for t in achados:
        print("  ", t)
    print(f"\ngravado: {destino}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
