# Camada de BI — views de leitura

Isola o Power BI (e um futuro painel nativo) do schema interno da aplicação.
As regras de negócio do BI — o que conta como "relatório pendente", como se mede
o tempo de atendimento, o que é risco alto — moram em um lugar só:
[`views_bi.sql`](views_bi.sql).

## Como é aplicado

O arquivo `.sql` é a **fonte única da verdade** e está embutido no assembly
(`EmbeddedResource` no `.csproj`). O `Infrastructure/Seeders/ViewsBiSeeder.cs` o
executa **a cada inicialização da API**, logo depois das migrations.

Como todo o script é `CREATE OR REPLACE`, rodar de novo é seguro. Na prática:

- **Para alterar um indicador**, edite o `.sql` e reinicie a API. Não precisa de
  migration nova.
- **Se as views falharem**, a API sobe assim mesmo e registra o erro no log —
  relatório não pode derrubar o atendimento.

Para aplicar à mão, contra qualquer banco:

```bash
psql -h localhost -U postgres -d sig-defesa-civil -f Scripts/BI/views_bi.sql
```

> **Atenção ao alterar uma view existente:** `CREATE OR REPLACE VIEW` não permite
> remover nem renomear colunas, só acrescentar ao final. Se você mudar a lista de
> colunas, derrube a view antes: `DROP VIEW IF EXISTS vw_bi_xxx CASCADE;`

## Conectando o Power BI

O Power BI conecta **direto no PostgreSQL**, sem intermediário — não existe MCP
nem API no meio. Use o conector nativo *PostgreSQL database*, servidor
`host:5432`, banco `sig-defesa-civil`, e importe apenas as views `vw_bi_*`.

Crie um usuário **só de leitura**. Nunca aponte o BI para o `postgres`:

```sql
CREATE ROLE bi_leitura LOGIN PASSWORD 'troque_esta_senha';
GRANT CONNECT ON DATABASE "sig-defesa-civil" TO bi_leitura;
GRANT USAGE ON SCHEMA public TO bi_leitura;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO bi_leitura;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO bi_leitura;
```

Comece carregando `vw_bi_ocorrencias` — quase todo indicador sai dela sem join.

## LGPD

**Nenhuma view expõe dado pessoal do solicitante** (nome, CPF, RG, e-mail,
telefone). O BI trabalha com protocolo, território, datas e classificações. Se
for preciso identificar o cidadão, isso passa pelo endpoint de revelação da API,
que registra o acesso em `log_acesso_lgpd` — não por aqui.

Ocorrências com `DeletedAt` preenchido (soft-delete) ficam fora de todas as views.

## Dicionário das views

| View | O que entrega | Grão |
|---|---|---|
| `vw_bi_ocorrencias` | **Fato principal.** Território, risco, população exposta, tempos, situação do relatório | 1 ocorrência |
| `vw_bi_vistorias` | Vistorias incluindo revisitas, com duração da visita e tamanho da equipe | 1 vistoria |
| `vw_bi_vistoria_multivalorado` | Campos de múltipla seleção em formato longo (`campo`, `valor`) | 1 valor |
| `vw_bi_indicadores` | Cartões do topo do painel | 1 linha |
| `vw_bi_serie_mensal` | Volume e desempenho por mês de abertura | 1 mês |
| `vw_bi_sazonalidade` | Distribuição por mês do ano (período chuvoso) | 1 mês do ano |
| `vw_bi_tempo_resposta` | Distribuição do tempo abertura → vistoria por faixa | 1 faixa |
| `vw_bi_bairros` | Consolidado por bairro, grafia unificada | 1 bairro |
| `vw_bi_backlog` | Fila operacional com envelhecimento e score de prioridade | 1 ocorrência aberta |
| `vw_bi_carga_vistoriador` | Carga de campo contando os 4 membros da equipe | 1 vistoriador |
| `vw_bi_tipo_risco` | Ranking das causas, agrupado por chave normalizada | 1 tipo |
| `vw_bi_populacao_exposta` | Moradores, crianças, idosos e PcD por bairro e grau | bairro × grau |
| `vw_bi_encaminhamentos` | Encaminhamentos por órgão, com taxa de retorno | 1 órgão |
| `vw_bi_notificados` | Entrega do relatório por forma de recebimento | forma × mês |
| `vw_bi_qualidade_dados` | Preenchimento de cada campo — **se atualiza sozinha** | 1 campo |

## Decisões que valem conhecer

**Normalização de bairro.** A extensão `unaccent` exige superusuário e não está
instalada no banco. `fn_bi_normaliza()` usa `translate()` — sem privilégio
especial. Sem isso, *Nossa Senhora de Fátima* e *Nossa Senhora DE Fatima* eram
dois bairros: 194 variantes viraram 170 bairros reais.

**"Não informado" vira NULL.** `fn_bi_valor()` converte os marcadores de ausência
da importação em NULL, para que não sejam contados como se fossem uma categoria.
**"Não constatado" não entra nessa regra** — é um resultado legítimo de vistoria.

**Datas impossíveis.** A carga histórica tem 31 registros com data de vistoria
inconsistente (um deles no ano 205). Sem filtro, a média de atendimento vira −600
dias. `dias_ate_vistoria` só considera o intervalo de 0 a 365; o valor cru fica em
`dias_ate_vistoria_bruto` para auditoria.

**Situação do relatório.** A planilha tinha a coluna `STATUS_RELATORIO`; o modelo
não guarda esse campo. `situacao_relatorio` é derivada: relatório assinado ou
encaminhamento final ou status `ENCERRADA` → *Concluído*; vistoria feita sem nada
disso → *Pendente*. É a definição usada tanto pelo painel quanto pelo Power BI.

**Carga de vistoriador conta os 4 membros.** Contar só o `Vistoriador1Id`
subestima a carga real de campo de quem atua como apoio.

## Estado dos dados (28/07/2026)

Consulte `vw_bi_qualidade_dados` para o número atual. No momento em que a camada
foi criada:

| Situação | Campos |
|---|---|
| **Confiável** | Bairro (99,1%), data de vistoria consistente (97,2%), interdição (81,3%) |
| **Parcial** | Grau de risco constatado (64,8%), coordenada geográfica (0,1%) |
| **Sem dado** | Tipo de risco, remoção, regime de ocupação, caracterização do local, composição familiar, moradores por imóvel, avaliação de risco (etapa 2), encaminhamento final (etapa 6), notificados |

As views dos campos sem dado **já estão prontas e retornam zero linhas sem
quebrar**. Passam a produzir resultado sozinhas conforme o sistema entrar em uso —
`vw_bi_qualidade_dados` serve para acompanhar essa transição.

Uma exceção que vale registrar: o **tipo de risco já tem dado**. A coluna
`TipoRisco` está vazia, mas o multivalorado `TipificacaoOcorrencia` veio
preenchido na importação (1.085 valores, 66 tipos distintos após normalizar).
Os textos são livres e sujos, por isso o agrupamento é sempre pela chave
normalizada — nunca pela grafia crua.

## Painel de referência

O desenho dos indicadores segue o painel publicado em
<https://claude.ai/code/artifact/4ae1d7f8-a718-47e6-9406-7083bfff217f>, que
organiza as análises na ordem em que a coordenação decide: o que está parado,
quanto tempo levamos, onde o risco se concentra, quando ele chega, com que equipe.
