# Importação da planilha histórica de ocorrências

Importa as abas `OCORRENCIAS_2025` / `OCORRENCIAS_2026` (com enriquecimento da
aba `RELATORIOS PRONTOS 2025`) para o banco do SIG-Defesa Civil, preservando o
número original da planilha como protocolo (ex.: `2025001`) e as datas reais.

## Fluxo

### 1. Gerar/validar o de-para de vistoriadores

Na primeira execução, o script gera `mapeamento_vistoriadores.csv` com uma
proposta. **Edite a coluna `nome_canonico`** unificando variações da mesma
pessoa (ex.: `PAULO ROGERIO`, `PAULO R`, `PAULO` → `Paulo Rogério`) e rode de
novo. Linhas com o mesmo `nome_canonico` viram uma única conta.

### 2. Gerar o SQL e as prévias (na sua máquina)

```powershell
cd "SIG-Defesa-Civil.API\Scripts\Importacao"
python importar_planilha.py "C:\Users\lucio\Downloads\PLANILHAS DE OCORRENCIAS.xlsx"
```

Saídas em `out/` (não versionadas — contêm dados pessoais):
- `import.sql` — SQL idempotente (rodar 2× não duplica nada)
- `previa_ocorrencias.csv` — revise antes de aplicar
- `normalizacoes.csv` — de-para de valores (grau, interdição, status)
- `rejeitadas.csv` — linhas puladas e motivo

### 3. Aplicar no servidor

```bash
# 3.1 Copie o import.sql para o servidor (ex.: scp) e vá à pasta do compose
scp out/import.sql usuario@servidor:/tmp/import.sql
cd /caminho/no/servidor/SIG-Defesa-Civil.API

# 3.2 BACKUP antes de tudo
docker compose exec db pg_dump -U defesacivil defesacivil > backup_pre_import_$(date +%Y%m%d).sql

# 3.3 Aplique (uma transação única; qualquer erro reverte tudo)
docker compose exec -T db psql -U defesacivil -d defesacivil -v ON_ERROR_STOP=1 < /tmp/import.sql

# 3.4 Confira
docker compose exec db psql -U defesacivil -d defesacivil -c \
  "SELECT \"Status\", COUNT(*) FROM ocorrencias GROUP BY 1 ORDER BY 2 DESC;"
```

## Decisões de modelagem

| Planilha | Sistema |
|---|---|
| `N_DA_VISTORIA` (2025001) | `Protocolo` literal — nunca colide com o formato novo `AAAA-NNNN` |
| REALIZADA + relatório CONCLUIDO/DISPENSÁVEL | status `ENCERRADA` |
| REALIZADA + relatório PENDENTE | status `VISTORIA_REALIZADA` |
| Vistoria PENDENTE | status `VISTORIA_SOLICITADA` (agendamento ATIVO — aparece na agenda) |
| GRAU_RISCO com variações | normalizado p/ enum; não mapeável → "Não constatado"/"Não informado" (catálogo) |
| Campos sem fonte (edificação, estrutura…) | "Não informado" (opção do catálogo) |
| VISTORIADORES (texto livre) | contas `VISTORIADOR` **desativadas** (V1/V2; excedentes na observação) |
| Solicitante sem CPF | conta CIDADAO com e-mail placeholder `sol.<protocolo>@importado.local` |
| OBSERVAÇÃO / DESPACHOS / RESPOSTA / conclusão do relatório | concatenados em `Observacoes` da vistoria |

Registros criados são atribuídos ao usuário de sistema
`importacao@sig.defesacivil.local` (ATENDENTE, desativado).
