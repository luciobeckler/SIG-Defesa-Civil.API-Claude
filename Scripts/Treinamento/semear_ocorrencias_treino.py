"""Semeia as 6 ocorrências do treinamento da equipe da Defesa Civil.

Uso:
    python semear_ocorrencias_treino.py <url-base>

    # ambiente de treino no servidor
    python semear_ocorrencias_treino.py http://treino.179.106.96.58.sslip.io:8081
    # ou contra a API local, para ensaiar o treinamento antes
    python semear_ocorrencias_treino.py https://localhost:7180

Os dados são **fictícios de propósito**: nomes inventados, CPFs inválidos e
telefones fora de faixa real. Os bairros e o tipo de problema são de Sabará,
para o exercício parecer o dia a dia sem usar o caso de ninguém.

As seis cobrem, juntas, o que o roteiro de treinamento exercita:
duas sem CPF (abertura por telefone), uma sem descrição (chamado relâmpago),
uma emergência, e seis bairros diferentes para o filtro ter o que filtrar.
"""
import json
import ssl
import sys
import urllib.error
import urllib.request
import uuid
import zlib

# ── As seis ocorrências ──────────────────────────────────────────────────────
OCORRENCIAS = [
    {
        "rotulo": "1 · Trinca estrutural — caso completo",
        "cidadao": {"nome": "Marlene Figueiredo Ramos", "celular": "31977010001",
                    "cpf": "11122233396", "email": "marlene.ensaio@exemplo.test",
                    "rg": "MG-11.222.333", "orgaoEmissor": "SSP/MG"},
        "local": {"endereco": "Rua Dom Pedro II", "numero": "412", "bairro": "Centro",
                  "cep": "34505000", "cidade": "Sabará", "uf": "MG",
                  "referencia": "Ao lado da padaria", "numeroIptu": "0145220"},
        "descricao": ("Trinca larga na parede dos fundos, aumentando desde as chuvas da "
                      "semana passada. A moradora relata barulho de estalo à noite."),
    },
    {
        "rotulo": "2 · Risco em encosta — emergência",
        "cidadao": {"nome": "Josué Barbosa de Lima", "celular": "31977010002",
                    "cpf": "22233344457"},
        "local": {"endereco": "Rua das Acácias", "numero": "77", "bairro": "Esplanada",
                  "cep": "34515000", "cidade": "Sabará", "uf": "MG",
                  "referencia": "Última casa da subida, após a curva"},
        "descricao": ("Barranco nos fundos do lote cedeu cerca de dois metros após a "
                      "chuva. Há terra sobre o quintal e risco para a casa vizinha."),
    },
    {
        "rotulo": "3 · Sem CPF — chamado por telefone",
        "cidadao": {"nome": "Terezinha Alves Mota", "celular": "31977010003"},
        "local": {"endereco": "Rua Cel. Afonso Pena", "bairro": "Siderúrgica",
                  "cidade": "Sabará", "uf": "MG",
                  "referencia": "Em frente ao campo de futebol"},
        "descricao": ("Muro de arrimo com embarrigamento visível, separando o lote da "
                      "via. A moradora não soube informar o documento no atendimento."),
    },
    {
        "rotulo": "4 · Sem CPF e sem descrição — chamado relâmpago",
        "cidadao": {"nome": "Antônio Carlos Siqueira", "celular": "31977010004"},
        "local": {"endereco": "Rua Projetada A", "bairro": "Roça Grande",
                  "cidade": "Sabará", "uf": "MG"},
        "descricao": None,
    },
    {
        "rotulo": "5 · Margem de córrego — alagamento",
        "cidadao": {"nome": "Rosângela Pires do Nascimento", "celular": "31977010005",
                    "cpf": "33344455518", "email": "rosangela.ensaio@exemplo.test"},
        "local": {"endereco": "Rua Beira Rio", "numero": "90", "complemento": "Fundos",
                  "bairro": "Pompéu", "cep": "34525000", "cidade": "Sabará", "uf": "MG",
                  "referencia": "Casa de esquina, próxima à ponte"},
        "descricao": ("A água do córrego invadiu o quintal e alcançou a soleira na "
                      "última chuva forte. Morador pede avaliação antes do período chuvoso."),
    },
    {
        "rotulo": "6 · Árvore com risco de queda",
        "cidadao": {"nome": "Valdir Monteiro da Cruz", "celular": "31977010006",
                    "cpf": "44455566679", "telefone": "3135550006"},
        "local": {"endereco": "Rua São Francisco", "numero": "233", "bairro": "Morro da Cruz",
                  "cep": "34535000", "cidade": "Sabará", "uf": "MG",
                  "referencia": "Próximo ao ponto de ônibus"},
        "descricao": ("Árvore de grande porte inclinada sobre a rede elétrica e o telhado. "
                      "Raízes expostas após a obra da calçada."),
    },
]


def png_minimo():
    """PNG 1x1 — o comprovante e a foto são obrigatórios na abertura."""
    def bloco(tipo, dados):
        c = tipo + dados
        return len(dados).to_bytes(4, "big") + c + zlib.crc32(c).to_bytes(4, "big")
    return (b"\x89PNG\r\n\x1a\n"
            + bloco(b"IHDR", (1).to_bytes(4, "big") + (1).to_bytes(4, "big")
                    + bytes([8, 2, 0, 0, 0]))
            + bloco(b"IDAT", zlib.compress(b"\x00\xff\xff\xff"))
            + bloco(b"IEND", b""))


def abrir(base, ctx, dados_ocorrencia):
    corpo_json = {"cidadao": dados_ocorrencia["cidadao"], "local": dados_ocorrencia["local"]}
    if dados_ocorrencia["descricao"]:
        corpo_json["descricaoProblema"] = dados_ocorrencia["descricao"]

    lim = "----" + uuid.uuid4().hex
    corpo = ("--" + lim + "\r\n"
             'Content-Disposition: form-data; name="Dados"\r\n\r\n'
             + json.dumps(corpo_json, ensure_ascii=False) + "\r\n").encode()
    for campo, arquivo in (("Comprovante", "comprovante.png"), ("Fotos", "foto.png")):
        corpo += ("--" + lim + "\r\n"
                  'Content-Disposition: form-data; name="' + campo + '"; '
                  'filename="' + arquivo + '"\r\n'
                  "Content-Type: image/png\r\n\r\n").encode() + png_minimo() + b"\r\n"
    corpo += ("--" + lim + "--\r\n").encode()

    req = urllib.request.Request(
        f"{base}/api/v1/ocorrencias", data=corpo, method="POST",
        headers={"Content-Type": f"multipart/form-data; boundary={lim}"})
    try:
        with urllib.request.urlopen(req, context=ctx, timeout=90) as r:
            return r.status, json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()[:300]


def main():
    if len(sys.argv) != 2:
        print(__doc__)
        return 1
    base = sys.argv[1].rstrip("/")
    # O ambiente local usa certificado de desenvolvimento.
    ctx = ssl._create_unverified_context() if base.startswith("https") else None

    print(f"Semeando 6 ocorrências de treinamento em {base}\n")
    criadas, falhas = [], []
    for o in OCORRENCIAS:
        status, resp = abrir(base, ctx, o)
        if status == 201:
            protocolo = resp.get("dados", {}).get("protocolo")
            criadas.append((o["rotulo"], protocolo))
            print(f"  [ok]    {o['rotulo']:48s} -> {protocolo}")
        else:
            falhas.append((o["rotulo"], status, resp))
            print(f"  [FALHA] {o['rotulo']:48s} -> {status} {str(resp)[:120]}")

    print(f"\n{len(criadas)} criadas, {len(falhas)} falhas")
    if criadas:
        print("\nProtocolos para anotar no quadro do treinamento:")
        for rotulo, protocolo in criadas:
            print(f"  {protocolo}  {rotulo}")
    return 0 if not falhas else 1


if __name__ == "__main__":
    sys.exit(main())
