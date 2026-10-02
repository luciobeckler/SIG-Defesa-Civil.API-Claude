"""Aplica na aba OCORRENCIAS o de-para de vistoriadores revisado à mão.

A coluna NOME NORMALIZADO da aba DE-PARA VISTORIADORES é a autoridade: é onde o
usuário decide se "Douglas" e "Douglas Martins" são a mesma pessoa. O
normalizar_planilha.py não sabe disso — ele recria o de-para pelas próprias
regras e sobrescreveria a revisão. Por isso este passo é separado.

Idempotente: rodar de novo não muda nada, porque o de-para é reescrito com os
mesmos valores.

Uso:
    python aplicar_depara_vistoriadores.py [planilha.xlsx]
"""
import collections
import re
import shutil
import sys
import unicodedata
from pathlib import Path

import openpyxl

PADRAO = Path(r"C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA_v2.xlsx")
NA = "N/A"

# Mesmas regras do normalizar_planilha.py — replicadas porque aquele módulo abre
# a planilha crua ao ser importado, e ela já não está no disco.
CORRECAO = {
    "JOANATAS": "JONATAS", "PRICILLA": "PRISCILLA", "YASMIM": "YASMIN",
    "YASMIM RIBEIRO": "YASMIN RIBEIRO", "ROGERIO E": "ROGERIO",
    "DOUGLAS M": "DOUGLAS MARTINS", "PEDRO P": "PEDRO PAULO",
    "LEANDRO S": "LEANDRO SANTOS", "PAULO R": "PAULO ROGERIO",
    "RAFAEL A": "RAFAEL ALMEIDA", "LEANDRO JESUS": "LEANDRO DE JESUS",
}

# Rótulos que não são pessoas — nunca viram conta de vistoriador
NAO_PESSOA = {"DEMAIS SECRETARIAS", "NOVA LIMA", "SECRETARIA DE OBRAS",
              "DEFESA CIVIL", "SEC. OBRAS", "OBRAS",
              "NAO ESPECIFICADO", "NAO INFORMADO"}


def chave(s):
    s = unicodedata.normalize("NFKD", str(s)).encode("ascii", "ignore").decode()
    return " ".join(s.upper().split())


def titulo(s):
    s = " ".join(str(s).split())
    if not s:
        return ""
    miu = {"DE", "DA", "DO", "DAS", "DOS", "E"}
    partes = []
    for i, p in enumerate(s.split()):
        u = chave(p)
        if u in miu and i > 0:
            partes.append(p.lower())
        elif len(p) <= 3 and p.isupper() and u not in miu:
            partes.append(p)
        else:
            partes.append(p.capitalize())
    return " ".join(partes)


def nome_antigo(bruto):
    """Reproduz o nome que o normalizador gerou antes da revisão manual."""
    p = chave(bruto)
    p = re.sub(r"^(SGT|CB|SD)\.?\s+", "", p)
    p = re.sub(r"\s*\((BH|CONTAGEM|NOVA LIMA)\)\s*", "", p)
    p = re.sub(r"\s+(BH|CONTAGEM)$", "", p)
    p = p.strip(" .-")
    if not p:
        return ""
    p = CORRECAO.get(p, p)
    return titulo(p)


def main():
    caminho = Path(sys.argv[1]) if len(sys.argv) > 1 else PADRAO
    if not caminho.exists():
        sys.exit(f"Planilha não encontrada: {caminho}")

    wb = openpyxl.load_workbook(caminho)
    dp = wb["DE-PARA VISTORIADORES"]
    oc = wb["OCORRENCIAS"]

    # ── Constrói antigo → novo, detectando ambiguidade ──────────────────────
    destinos = collections.defaultdict(set)
    descartar = set()
    for row in dp.iter_rows(min_row=2, values_only=True):
        bruto, novo = (list(row) + [None] * 2)[:2]
        if not bruto:
            continue
        antigo = nome_antigo(bruto)
        if not antigo:
            continue
        if chave(bruto) in NAO_PESSOA or (novo and chave(novo) in NAO_PESSOA):
            descartar.add(antigo)
            continue
        if novo and str(novo).strip():
            destinos[antigo].add(str(novo).strip())

    ambiguos = {a: d for a, d in destinos.items() if len(d) > 1}
    if ambiguos:
        print("[ERRO] O mesmo nome aponta para pessoas diferentes:")
        for a, d in ambiguos.items():
            print(f"   {a} -> {sorted(d)}")
        sys.exit("Resolva na aba DE-PARA VISTORIADORES e rode de novo.")

    mapa = {a: next(iter(d)) for a, d in destinos.items()}

    # ── Aplica nas colunas VISTORIADOR_1..4 ─────────────────────────────────
    cab = [c.value for c in oc[1]]
    cols = [cab.index(f"VISTORIADOR_{i}") + 1 for i in range(1, 5)]

    trocas = collections.Counter()
    linhas_alteradas = 0
    excedentes = 0

    for linha in range(2, oc.max_row + 1):
        atuais = []
        for c in cols:
            v = oc.cell(linha, c).value
            if v and str(v).strip() != NA:
                atuais.append(str(v).strip())

        novos = []
        for v in atuais:
            if v in descartar:
                trocas[f"{v} → (descartado)"] += 1
                continue
            n = mapa.get(v, v)
            if n != v:
                trocas[f"{v} → {n}"] += 1
            # dedup: dois apelidos da mesma pessoa colapsam em um
            if n not in novos:
                novos.append(n)

        if len(novos) > 4:
            excedentes += 1
            novos = novos[:4]

        if novos != atuais:
            linhas_alteradas += 1

        for i, c in enumerate(cols):
            oc.cell(linha, c).value = novos[i] if i < len(novos) else NA

    # ── Grava ───────────────────────────────────────────────────────────────
    tmp = caminho.with_suffix(".tmp.xlsx")
    wb.save(tmp)
    try:
        shutil.move(str(tmp), str(caminho))
        destino = caminho
    except PermissionError:
        destino = caminho.with_name(caminho.stem + "_atualizada.xlsx")
        shutil.move(str(tmp), str(destino))

    distintos = set()
    for linha in range(2, oc.max_row + 1):
        for c in cols:
            v = oc.cell(linha, c).value
            if v and str(v).strip() != NA:
                distintos.add(str(v).strip())

    print(f"Gravado: {destino}")
    print(f"  linhas alteradas   : {linhas_alteradas}")
    print(f"  vistoriadores antes: 80")
    print(f"  vistoriadores agora: {len(distintos)}")
    if excedentes:
        print(f"  linhas que passaram de 4 após unificar: {excedentes} (excedente cortado)")
    print()
    print("  principais trocas:")
    for t, n in trocas.most_common(12):
        print(f"     {n:4d}x  {t}")


if __name__ == "__main__":
    main()
