"""Gera a planilha num arquivo temporário e só então entrega no Desktop.

O Excel trava o arquivo enquanto ele está aberto; gerar direto no destino
falharia no meio. Assim a geração nunca fica pela metade — se a entrega falhar,
o conteúdo já existe e é só copiar.
"""
import io, os, re, runpy, shutil

AQUI = os.path.dirname(os.path.abspath(__file__))
FONTE = os.path.join(AQUI, "normalizar_planilha.py")
TEMP = os.path.join(AQUI, "_saida.xlsx")
DESTINO = r"C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA.xlsx"
ALTERNATIVO = r"C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA_v2.xlsx"

codigo = io.open(FONTE, encoding="utf-8").read()
linha_saida = 'SAIDA = r"' + TEMP + '"'
codigo = re.sub(r"^SAIDA = .*$", lambda _: linha_saida, codigo, count=1, flags=re.M)

runner = os.path.join(AQUI, "_tmp_run.py")
io.open(runner, "w", encoding="utf-8").write(codigo)
runpy.run_path(runner, run_name="__main__")

print()
try:
    shutil.copyfile(TEMP, DESTINO)
    print(">>> entregue em", DESTINO)
except PermissionError:
    shutil.copyfile(TEMP, ALTERNATIVO)
    print(">>> destino travado pelo Excel; entregue em", ALTERNATIVO)
