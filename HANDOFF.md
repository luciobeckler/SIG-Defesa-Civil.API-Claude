# HANDOFF — estado da sessão (31/07/2026)

Documento de passagem de contexto para continuar em uma nova sessão do Claude Code.
Leia junto com `CLAUDE.md` (arquitetura, como rodar, convenções). Datas em absoluto.

> **Comece por aqui:** o código está commitado e no GitHub (`fc65c30` + este commit).
> O `import.sql` está gerado e testado contra o banco local. **Falta apenas o deploy** —
> ver "Pendências → 3". Nada disso existe no servidor ainda: ele faz `git pull` de `master`.

---

## ✅ Entregue nesta sessão (29–31/07/2026)

Tudo compilando: backend `dotnet build` 0 erros, frontend `ng build` OK.

### 1. Solicitante deixou de ser usuário (29/07)
A abertura pública criava uma conta `CIDADAO` por CPF — 1.078 contas poluindo `usuarios`, e
reabrir com o mesmo CPF reescrevia nome/contato das ocorrências antigas. Agora os dados moram
em colunas `Solicitante*` da própria `ocorrencias` (owned entity `SolicitanteOcorrencia`).
`ocorrencias.CriadoPorId`, `arquivos.enviado_por` e `log_acesso_lgpd.UsuarioId` viraram nullable.
Migration `20260729002541_SolicitanteEmbutidoNaOcorrencia` copia os dados e apaga os `CIDADAO`.
**DTOs não mudaram de forma — o frontend não precisou de alteração.**
Backup pré-mudança: `TCC/backups/sig-defesa-civil_pre-solicitante_20260728.dump`.

### 2. Camada de BI em SQL (30/07)
- **`Scripts/BI/views_bi.sql`** — fonte única da verdade. 16 views `vw_bi_*` + 6 funções `fn_bi_*`.
  Toda regra de negócio do BI mora aqui (o que é "relatório pendente", como se mede tempo,
  o que conta como risco alto). Nenhuma view expõe PII do solicitante (LGPD).
- **`Infrastructure/Seeders/ViewsBiSeeder.cs`** — aplica o `.sql` (EmbeddedResource no `.csproj`)
  a cada startup. `CREATE OR REPLACE`, então reexecutar é seguro. Falha não derruba a API.
- **`Program.cs`**: `ViewsBiSeeder.RemoverAsync` → `AdminSeeder.SeedAsync` (migrations) → `ViewsBiSeeder.SeedAsync`.
  A ordem importa — ver "armadilhas" abaixo.
- Documentação: `Scripts/BI/README.md` (dicionário das views, conexão do Power BI, usuário read-only).

### 3. Projeto Power BI (30/07)
Em `C:\Users\lucio\Desktop\TCC\`:
```
SIG-Defesa-Civil-BI.pbip                 <- abre na Power BI Desktop, Salvar como → .pbix
SIG-Defesa-Civil-BI.SemanticModel/       <- TMDL: 2 tabelas + 30 medidas DAX
SIG-Defesa-Civil-BI.Report/              <- 4 páginas, 40 visuais, filtro de ano em todas
SIG-Defesa-Civil-BI.pbids                <- conexão avulsa
SIG-Defesa-Civil-BI-LEIAME.md            <- passo a passo + números esperados por ano
```
Gerador: `scratchpad/gerar_pbip.py` (não versionado — ver "arquivos fora do repo").
**O `.pbix` não pode ser escrito fora da Power BI Desktop** (container binário; a parte
`DataModel` é um modelo Analysis Services compilado). O PBIP é o formato de projeto em texto.

Tabelas: `Ocorrencias` (`vw_bi_ocorrencias`) e `Tipificacoes` (`vw_bi_tipificacao_ocorrencia`),
ambas com coluna `ano` — cada página filtra a própria tabela, sem relacionamento entre elas.

### 4. Tipificação multivalorada nas duas etapas (31/07)
`AvaliacaoRisco.TipificacaoInicial` era um enum único; virou `List<string>` (`text[]`), igual a
`Vistoria.TipificacaoOcorrencia`. Migration `20260731021344_TipificacaoInicialMultivalorada`.
Frontend: `ion-select multiple`, helper `labelOpcoes()`, e o pipe `tipificacao-label.pipe.ts`
reescrito (só conhecia 2 dos 14 tipos e devolvia "—" no resto).
Modelos gerados do OpenAPI editados à mão em `src/app/core/api-generated/model/` (4 arquivos).

### 5. Normalização da planilha histórica (31/07)
- Script: **`Scripts/Importacao/normalizar_planilha.py`** (reprodutível).
- Saída: **`C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA.xlsx`** — 8 abas.

| Aba | Linhas | Para quê |
|---|---|---|
| OCORRENCIAS | 1.112 | dados no formato do sistema, `N/A` nos vazios |
| DE-PARA VISTORIADORES | 104 | 12 marcados para decisão do usuário |
| DE-PARA BAIRROS | 193 | 193 grafias → 142 bairros reais |
| DE-PARA TIPIFICACOES | 64 | inclui os descartados por decisão |
| CATALOGO A CRIAR | 14 | opções que precisam existir antes da carga |
| PENDENCIAS | **0** | nada mais pede decisão por linha |
| AJUSTES APLICADOS | 940 | rastreabilidade do que foi resolvido sozinho |
| LINHAS DESCARTADAS | 13 | numerações pré-lançadas, sem nenhum campo preenchido |

**Decisões já tomadas pelo usuário e aplicadas:** só os 4 primeiros vistoriadores; só o primeiro
bairro quando a célula tem dois; nome que não é vistoriador sai; data de vistoria inconsistente
vira N/A (31 casos); documento que não tem 11 dígitos vira N/A (30 casos); linha sem data de
solicitação é descartada (13).

**8 tipificações descartadas** (motivo da solicitação, não tipo de risco): `ALUGUEL_SOCIAL`,
`AVALIACAO_DE_RISCO`, `CADASTRO_HABITACIONAL`, `DENUNCIA`, `INVASAO`, `RESPOSTA_DE_EMERGENCIA`,
`VISTORIA_CAUTELAR`, `VISTORIA_DE_OBRA`.
→ Efeito: ocorrências com tipificação caíram de 1.094 (98%) para **164 (14,7%)**. Não é erro —
é o dado real aparecendo: em 85% dos casos a planilha nunca registrou o tipo de risco.
O campo `EMERGENCIA` é apurado **antes** do descarte, então as 11 emergências continuam marcadas.

---

## 🧭 Migrations presentes (aplicadas no banco local)

```
20260616003434_AlterandoBanco                    (baseline achatado — cria todas as tabelas)
20260616022930_AdicionarDataAgendamento
20260623021449_OpcoesPersonalizadasVistoria      (integer[]→text[] nas colunas da vistoria)
20260710110926_EquipeVistoriaQuatroPessoas
20260714015752_NotificadosPropriedadeDaOcorrencia
20260729002541_SolicitanteEmbutidoNaOcorrencia   (solicitante embutido + DELETE dos CIDADAO)
20260731021344_TipificacaoInicialMultivalorada   (text→text[] com USING; derruba as views antes)
```

## 🗄️ Banco LOCAL agora

**1.116 ocorrências** — 1.034 ENCERRADA / 67 VISTORIA_REALIZADA / 11 VISTORIA_SOLICITADA / 4 ABERTA.
**98 usuários**: 96 VISTORIADOR + 1 ADMIN + 1 ATENDENTE, **nenhum CIDADAO**.
`avaliacoes_risco`, `encaminhamentos_finais` e `notificados` estão **vazias**.
Login: `admin@defesacivil.sabara.mg.gov.br`.

---

## ⚠️ Armadilhas descobertas (não repetir)

1. **Views de BI bloqueiam migrations.** O PostgreSQL recusa alterar o tipo de uma coluna lida por
   uma view. Por isso o `Program.cs` derruba as `vw_bi_*` antes das migrations. Migration aplicada
   à mão precisa derrubá-las ela mesma (ver a de 31/07 como modelo).
2. **`AlterColumn` do EF não gera `USING`.** Converter tipo em tabela populada falha. Escrever
   `migrationBuilder.Sql` com `USING` explícito.
3. **Medida DAX não pode ter o nome de uma coluna da mesma tabela** (comparação ignora maiúsculas).
   `Idosos` colidia com a coluna `idosos` e derrubava o projeto PBIP inteiro. Por isso as medidas
   de população usam prefixo "Total de".
4. **PBIP é intolerante a referência solta**: `queryGroup` não declarado, tema ausente, ou tipo de
   visual inexistente derrubam o arquivo todo. Só usar tipos comprovados: `card`,
   `clusteredColumnChart`, `slicer`, `tableEx`. O tema (`CY26SU04.json`) precisa existir
   fisicamente em `Report/StaticResources/SharedResources/BaseThemes/`.

---

## ⏳ Pendências

### 1. ✅ RESOLVIDO — importador da planilha normalizada
**`Scripts/Importacao/importar_normalizada.py`** substitui o `importar_planilha.py` para a carga
histórica. Lê a `PLANILHA_NORMALIZADA.xlsx` (não a crua), semeia as opções de catálogo antes da
carga e grava `TipificacaoInicial` como `text[]`. Mantém guarda de colisão de protocolo,
`--limpar`, avanço da sequence e idempotência.

```bash
cd Scripts/Importacao && python importar_normalizada.py --limpar
```

Saída testada contra o banco local em 02/08: 1.112 ocorrências, 1.112 localizações,
**164 avaliações de risco** (só as com tipificação real, após os descartes), 1.023 agendamentos,
963 vistorias, 299 notificados, 40 opções de catálogo, 103 vistoriadores.

> `importar_planilha.py` continua no repo mas **está obsoleto** — lê a planilha crua e refaz uma
> normalização antiga, sem os descartes. Não usar para carga nova.

### 2. Decisão do usuário — 12 vistoriadores de primeiro nome isolado
Na aba `DE-PARA VISTORIADORES`, coluna `OBSERVAÇÃO`. "Leandro" é o Leandro Santos ou o Leandro
de Jesus? Pesam bastante: Douglas 329, Rafael 210, Rogerio 185. O script **não adivinha** —
unir duas pessoas por engano é pior do que deixar separado. Editar a coluna
`NOME NORMALIZADO` e reprocessar.

Já corrigido automaticamente: `JOANATAS`→Jonatas, `PRICILLA`→Priscilla, `YASMIM`→Yasmin,
`PAULO R`→Paulo Rogerio, `LEANDRO S`→Leandro Santos, patentes (`SGT`) e cidades (`BH`, `(CONTAGEM)`).

### 3. Deploy em produção — roteiro acordado
Servidor: `ssh luciobeckler@179.106.96.58`. App externo `http://179.106.96.58:8081/`.
**Pasta do projeto no servidor: `~/app`** (não `~/SIG-Defesa-Civil.API`).

⚠️ `~/app/.git` é **propriedade do root** (deploy anterior feito como root), enquanto `~/app`
pertence a `luciobeckler`. `git pull` como usuário normal falha por permissão, e o git ainda pode
reclamar de *dubious ownership*. Corrigir uma vez — o usuário tem a senha de root (`su` funciona):
```bash
su -c 'chown -R luciobeckler:luciobeckler /home/luciobeckler/app'
```
O `sudoers` do servidor tem `Defaults insults` ligado: senha errada devolve mensagens de brincadeira
(estilo HAL 9000), que **não** significam falta de permissão. `sudo -l` mostra o que é permitido.
Decisão do usuário: **apagar o banco de produção e subir os dados normalizados**.

> Apagar o banco **resolve** a pendência crítica antiga (baseline achatado + `integer[]→text[]`
> em tabela populada): com `down -v` o volume some e as migrations rodam do zero.

```
Fase 1  commit + push para master                  [LOCAL, ✅ FEITO]
Fase 2  npm run build + scp de www/ → servidor     [LOCAL]
Fase 3  pg_dump + tar de /var/sig-defesa-civil/arquivos   [SERVIDOR, backup]
Fase 4  git pull && docker compose down -v         [SERVIDOR, IRREVERSÍVEL]
Fase 5  docker compose up -d --build               [SERVIDOR]
Fase 6  scp import.sql + psql -v ON_ERROR_STOP=1   [SERVIDOR]
Fase 7  SELECT * FROM vw_bi_indicadores            [SERVIDOR, conferência]
```
`ON_ERROR_STOP=1` é essencial — sem ele o psql engole erros e deixa carga parcial.
O frontend não está versionado no servidor; `www/` vai por `scp`.

### 4. Artigo SBC (TCC)
Esqueleto `.tex` em `C:\Users\lucio\Downloads\SIG_Defesa_Civil___Lýcio_Beckler_Passos\artigo-sig-defesa-civil.tex`.
Relato de experiência (as-is → to-be), com marcadores `% [COLETAR: ...]`.

**Achados desta base que servem ao artigo:**
- O gargalo não é a vistoria: das 82 em aberto, **67 já tiveram vistoria** e travaram no
  relatório/encerramento — a mais antiga há 571 dias.
- A média de 22 dias engana: a **mediana é 6**. 42% saem em até 3 dias; 185 casos passam de 30.
- **73% da demanda cai entre outubro e março.** Março 270, setembro 10.
- 2026 atende mais rápido que 2025 (mediana 5 vs 8,5 dias) com mais volume — verificar se é ganho
  de processo ou efeito do ano não ter fechado.

### 5. TODO pequeno
No modo offline da listagem os badges das abas ficam ocultos (o `/resumo` não é buscado sem rede).
Sugerido calcular do cache local — não implementado.

---

## 📁 Scripts auxiliares (todos versionados)

```
Scripts/BI/views_bi.sql                    <- fonte única das views (aplicada no startup)
Scripts/BI/gerar_pbip.py                   <- regera o projeto Power BI em Desktop\TCC
Scripts/Importacao/normalizar_planilha.py  <- normaliza a planilha histórica
Scripts/Importacao/gerar_e_entregar.py     <- roda o normalizador e entrega no Desktop
Scripts/Importacao/importar_planilha.py    <- gera o import.sql (ainda lê a planilha CRUA)
```

`gerar_pbip.py` tem caminhos absolutos embutidos (Desktop do usuário e a pasta de temas da
instalação da Power BI Desktop, versão `2.156.951.0`). Se a versão mudar, ajustar `TEMAS_INSTALADOS`.

## 🔐 Segurança

A connection string de produção (Neon) apareceu em texto claro no terminal durante esta sessão.
**Rotacionar a senha antes de publicar o TCC.** Para o BI, usar usuário só-leitura — SQL pronto
em `Scripts/BI/README.md`.

## Serviços locais
Não sobrevivem a novas sessões. Para subir: `dotnet run --launch-profile https` (API, porta 7180)
e `npm start` (front, http://localhost:4200).
