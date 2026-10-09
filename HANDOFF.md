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

## 🔁 Recarga de produção só com 2026 (19/09/2026)

Decisão do usuário: **derrubar o banco de produção** (`docker compose down -v`) e recarregar só a
aba `OCORRENCIAS_2026` da planilha nova (`Downloads\PLANILHAS DE OCORRENCIAS (2).xlsx`).
Os `import.sql`/scripts em `Desktop\TCC\Scripts` e a `PLANILHA_NORMALIZADA_v2` são da carga anterior.

```bash
cd Scripts/Importacao
python normalizar_planilha.py "--entrada=C:\Users\lucio\Downloads\PLANILHAS DE OCORRENCIAS (2).xlsx" \
  --abas=OCORRENCIAS_2026 "--depara=C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA_v2.xlsx" \
  "--saida=C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA_2026.xlsx"
python importar_normalizada.py "C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA_2026.xlsx"
```
Resultado (ensaiado num PostgreSQL descartável + API real, endpoints 200): **775 ocorrências**
(`2026-0001`…`2026-0775`), 707 agendamentos, 670 vistorias, 37 vistoriadores, 30 opções de catálogo.

O que mudou nos scripts por causa da planilha nova:
- A coluna `TIPIFICACAO_OCORRENCIA` de 2026 é quase sempre "AVALIAÇÃO DE RISCO" (descartada);
  a tipificação real está em **`SUBTIPO DA OCORRÊNCIA`** e o tipo de risco em
  **`CLASSIFICAÇÃO DE RISCO`** → viraram `TIPIFICACAO_*` e a nova coluna `TIPO_RISCO`.
- Os **8 descartes de tipificação** agora estão no código (`DESCARTAR_TIPIFICACAO`) — antes foram
  feitos à mão na v2 e um reprocessamento os traria de volta.
- `--depara` herda o de-para de vistoriadores revisado; linhas da v2 que só repetiam o padrão
  cedem lugar às correções de digitação (Joantas→Jonatas, Tasmin→Yasmin…).
- Interdição de 2026 usa outro vocabulário ("NÃO INTERDITADO", "INTERDITADO", "INTERDIÇÃO PARCIAL").
- 15 pedidos da aba 2026 datados de jan/fev de **2025** (erro de virada de ano) têm o ano corrigido.
- `import.sql` começa com `SET client_encoding = 'UTF8'` — o psql do Windows lia como WIN1252.
- `NOTIFICAÇÃO` em 2026 é "NÃO EMITIDA"/"AUSÊNCIA DO SOLICITANTE" → **0 notificados** é o dado real.

**Carga de 2025 (19/09/2026, depois da de 2026, sem `--limpar`):** aba histórica, já finalizada.
A aba RELATORIOS PRONTOS 2025 só existe na planilha original (`Downloads\PLANILHAS DE OCORRENCIAS.xlsx`).
```bash
python normalizar_planilha.py "--entrada=C:\Users\lucio\Downloads\PLANILHAS DE OCORRENCIAS (2).xlsx" \
  --abas=OCORRENCIAS_2025 "--depara=C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA_v2.xlsx" \
  "--relatorios=C:\Users\lucio\Downloads\PLANILHAS DE OCORRENCIAS.xlsx" --encerrar \
  "--saida=C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA_2025.xlsx"
python importar_normalizada.py "C:\Users\lucio\Desktop\TCC\PLANILHA_NORMALIZADA_2025.xlsx"
```
Em produção: 434 ocorrências (`2025-0001`…`2025-0434`, todas ENCERRADA), 380 vistorias, 256 notificados,
233 encaminhamentos finais; 2026 intacto (hash conferido antes/depois). Total: 1.209 ocorrências.
Novidades nos scripts: despachos/respostas/destinos vão para `encaminhamentos_finais` (enum guardado como
índice `int[]`); moradores por grupo (antes "4 ADULTOS, 2 CRIANÇAS" virava 42); fissuras/rachaduras →
TRINCAS; WhatsApp conta como EMAIL; observação de linha sem vistoria vai para a descrição.

⚠️ Arquivos em disco ficam em `/arquivos/<protocolo>/...` e a aba Documentos lista as pastas **do
disco**. Numa recarga, mover o conteúdo antigo de `/var/sig-defesa-civil/arquivos` (menos
`templates`) — senão pastas de ocorrências antigas aparecem nas novas de mesmo protocolo.

---

## 📱 Offline por HTTP e APK Android (30/09/2026)

**O bloqueio real do offline não era HTTPS, era uma chamada.** Medido num build servido em
`http://192.168.100.167:8099` (origem insegura, igual em formato à de produção): `isSecureContext`
false, `crypto.randomUUID` **undefined** (`TypeError` ao chamar), `getRandomValues` e `indexedDB`
disponíveis, `serviceWorker` **ausente**. Como `local-database.service.ts` criava o `localId` com
`crypto.randomUUID()`, a gravação da vistoria offline morria antes de persistir qualquer coisa.

- Correção: `src/app/shared/utils/uuid.ts` (`uuidV4()` sobre `getRandomValues`), aplicada nos 4 usos
  (1 em `local-database.service.ts`, 3 em `file-storage.service.ts`). Bundle de produção não contém
  mais `randomUUID`. Em produção desde 01/10/2026 (`main-ZH2FCZIJ.js`;
  backup `~/backups/www_antes_uuid_20261001_0119.tgz`).
- Ainda **exige** contexto seguro, sem solução em código: abrir o sistema sem rede (service worker)
  e a captura de localização no navegador.

**APK Android** (`npx cap sync android` + `gradlew assembleDebug`): WebView serve de
`https://localhost`, que é contexto seguro — resolve UUID, abertura offline e localização sem mexer
no servidor.

- `capacitor.config.ts`: appId `br.gov.sabara.defesacivil`, appName "Defesa Civil Sabara",
  `android.allowMixedContent: true` (a API é HTTP e a página é https → conteúdo misto).
- `android/app/src/main/res/xml/network_security_config.xml`: cleartext liberado **só** para
  192.168.8.15 e 179.106.96.58; referenciado no manifesto.
- Manifesto: permissões de localização, câmera e leitura de imagens (o formulário de abertura usa os
  plugins nativos de câmera e geolocalização).
- `ApiBaseService` + `apiBaseInterceptor`: no app instalado as chamadas `/api/...` recebem prefixo
  absoluto, escolhido testando `environment.servidoresNativos` na ordem (IP interno, depois externo)
  a cada retorno de rede. No navegador o prefixo é vazio e nada muda.
- APK publicado em `~/app/www/app/defesa-civil-sabara-v1.0.apk` → baixável em
  `http://179.106.96.58:8081/app/defesa-civil-sabara-v1.0.apk` (testado de fora, 9,68 MB, 200).
  ⚠️ O deploy do frontend apaga `www/*`: **preservar ou recopiar `www/app/`** (ou usar
  `rsync --exclude app`).
- É build de depuração (assinada com a chave de debug): instala por sideload, mas para distribuição
  ampla convém gerar uma chave de publicação — a senha dela fica com o usuário.

---

## ✍️ Assinatura dos vistoriadores (01/10/2026)

Decisão do usuário: **uma assinatura por vistoriador** da equipe (até 4, com "quem assinou e quem
falta" na tela) e **coleta também offline**. Pode ser feita depois de a vistoria estar registrada.

Backend:
- `TipoArquivo.ASSINATURA_VISTORIADOR` → mesma pasta `Assinaturas`.
- `OcorrenciaService`: `SalvarAssinaturaVistoriadorAsync` valida que o vistoriador **está na equipe
  daquela vistoria** (senão 400) e delega a `SalvarAssinaturaInternaAsync`, extraída da assinatura do
  munícipe — as duas compartilham gravação e substituição por nome determinístico
  (`assinatura_vistoriador_{vistoriadorId}_vistoria_{vistoriaId}.png`).
- `POST /api/v1/ocorrencias/{id}/assinatura-vistoriador/{vistoriaId}/{vistoriadorId}` (multipart `arquivos`).
- `VistoriaDto` ganhou `Vistoriador1Id..4Id`; o **detalhe interno** passou a carregar a equipe
  (4 `ThenInclude`) e devolve ids, nomes e matrículas. `AcompanharAsync` (público) ficou **sem** a
  equipe de propósito.

Frontend:
- `AssinaturaPendente { path, tipo, vistoriadorId? }` na fila offline; `assinaturaPath` antigo
  continua sendo lido (aparelhos podem ter fila da versão anterior).
- `VistoriaOfflineService.submitAssinaturaVistoriador()` (online direto, senão fila) e
  `_enviarAssinatura` escolhe o endpoint conforme haja `vistoriadorId`.
- Detalhe: bloco "Assinaturas da Equipe" com contador, "Assinado"/"Assinatura pendente" e botões
  Ver/Assinar por pessoa.

Ensaiado de ponta a ponta contra a API real (PostgreSQL descartável): 12 verificações, incluindo
regressão da assinatura do munícipe, recusa de quem está fora da equipe e reassinatura sem duplicar
linha em `arquivos` nem arquivo em disco.

⚠️ O relatório `.docx` é **só texto** (placeholders): nenhuma assinatura entra nele, nem a do
munícipe. Colocar imagem no relatório é mudança separada.

**Em produção desde 02/10/2026:** backend `0d41598`, frontend `e4cc4ca` (bundle `main-ACADEMSP.js`),
APK **1.1** (`versionCode 2`, mesma chave da 1.0 → instala por cima) em
`http://179.106.96.58:8081/app/defesa-civil-sabara.apk` (nome estável) e `...-v1.1.apk` (versionado).
Backup anterior ao deploy: `~/backups/antes_assinatura_20261002_0324.dump`.
O deploy do frontend apaga `www/*`: **preservar `www/app/`** (é onde mora o APK).

⚠️ Chegar ao tablet exige **novo APK** (a tela é do frontend). A chave de publicação já existe
(`android/keystore.properties`, fora do git), então a atualização instala por cima preservando dados.

---

## 💾 Rascunho da vistoria e perdas de preenchimento (02/10/2026)

**Por que o preenchimento sumia.** Três causas, duas comprovadas no código:
- `ionViewWillEnter()` chamava `carregar()`, que faz `formAtivo.set(null)`: sair da tela (Documentos,
  botão voltar) e retornar fechava o formulário aberto. **Corrigido**: com formulário aberto, a tela
  não recarrega.
- Sessão de 8 h vencia em campo; o 401 levava ao login no meio da vistoria. **Corrigido**: 24 h,
  ajustável por `JWT_EXPIRACAO_HORAS` no compose. Atenção: `appsettings.Development.json` também
  fixava 8 h e mascarava a mudança em teste local.
- O sistema operacional encerra o app/aba em segundo plano por memória (típico após a câmera).
  Não há `location.reload()` no código e o `configChanges` do Android está completo — esta é
  inferência de plataforma, não achado de código. É o que o rascunho cobre.

**Rascunho** (`rascunhos_vistoria`, um por ocorrência e vistoriador, `ConteudoJson` em jsonb):
`RascunhoService` grava a cada campo (debounce 600 ms) sempre no aparelho e, com rede, no servidor;
`GET/PUT/DELETE /api/v1/ocorrencias/{id}/rascunho-vistoria`. Ao reabrir, restaura o mais recente
entre local e servidor. Apagado ao registrar a vistoria ou ao descartar.

Cuidados que custaram retrabalho: `reset()`/`patchValue()` disparam `valueChanges` e recriavam
rascunho vazio (usar `emitEvent: false`); abrir o formulário preenche `horarioInicio` e isso não
conta como conteúdo (`_temConteudo`); sem token os endpoints caíam no catch genérico e devolviam
500 em vez de 401.

**Em produção desde 02/10/2026:** backend `df16e23`, frontend `7ea872c` (bundle `main-BGSCMADV.js`),
APK **1.2** (`versionCode 3`). Backup: `~/backups/antes_rascunho_20261002_1809.dump`.

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
Servidor: `ssh luciobeckler@192.168.8.15` (interno) **ou** `luciobeckler@179.106.96.58` (externo) —
verificado em 02/08/2026: **os dois respondem na porta 22**, mesmo com a máquina do usuário em
outra faixa (`192.168.18.x`). App externo: `http://179.106.96.58:8081/`.
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

## 🔓 Acompanhamento público e filtro por protocolo (09/10/2026)

### 1. O CPF opcional tinha trancado o munícipe para fora — e aberto uma porta
`AcompanharAsync` comparava só o CPF. Ocorrência sem CPF guardava string vazia, então:

- o munícipe digitava o próprio CPF e levava **403** — sem caminho nenhum pela tela;
- qualquer texto sem dígito (`-`, `abc`) normalizava para vazio **e batia com o vazio gravado**,
  liberando a consulta a quem soubesse o protocolo — que é **sequencial**. A tela exigia 11 dígitos,
  mas o endpoint é público e aceita `curl`. Reproduzido e medido antes de corrigir.

Não nasceu com o CPF opcional: os registros importados das planilhas já entravam sem CPF.

Correção: `ConfereSolicitante` aceita **CPF, celular ou telefone fixo**, comparando só dígitos, e
**valor vazio nunca confere**. `TemComoConferir` separa o caso de não haver nada conferível, que
responde 403 com `ErrosRequisicoes.SEM_COMO_CONFERIR` e orienta procurar a Defesa Civil — antes caía
no catch genérico e virava **404**, dizendo "protocolo não encontrado" para protocolo que existe.
Vale igual para o download público do relatório. O parâmetro virou `identificacao`; **`cpf` continua
aceito** para não quebrar links salvos.

**Na base de produção:** dos 348 sem CPF, **335 têm telefone** e são resgatados; só **13** caem na
mensagem de balcão.

### 2. Filtro por protocolo não respondia depois da primeira busca
`textoDebounce$` era `Subject<void>`: todo `next()` emitia `undefined` e o `distinctUntilChanged()`
do pipe descartava tudo a partir da segunda. A busca funcionava **uma vez** e parava até recarregar
a página — valia também para bairro e CPF digitados. Anterior às mudanças desta semana (commits de
maio e junho). Agora o subject carrega o texto dos campos, que é o que o operador deveria comparar.

### Validação
20 verificações contra a API local (`scratchpad/testar_acompanhar.py` e `testar_sem_conferir.py`),
mais a cadeia RxJS exercitada com o rxjs do projeto: 3 termos distintos davam **1 busca** antes e
dão **3** agora. Em produção: telefone abre (200), traço recusa (`ACESSO_NEGADO`), número errado 403,
`?cpf=` antigo segue funcionando.

**Em produção desde 09/10/2026:** backend `21affd9`, frontend `633e62e` (bundle `main-Q4SUKDKA.js`),
APK **1.5** (`versionCode 6`, mesma chave). Backups: `~/backups/antes_acompanhar_20261009_1158.dump`
e `~/backups/www_antes_acompanhar_20261009_1159.tgz`.

⚠️ **O servidor ficou inalcançável por ~2 dias** (09/10 de manhã: SSH e 8081 sem resposta nos dois
IPs, com internet local funcionando). Voltou sozinho; `uptime` de 122 dias mostra que a máquina não
reiniciou — foi o link, não o servidor. O IP externo segue recusando a chave SSH: deploy só de dentro
da rede da prefeitura.

---

## 🧾 Obrigatórios mínimos, ficha de vistoria e eventos da agenda (06/10/2026)

### 1. Só nome, endereço (sem CEP) e telefone travam a abertura
Decisões do usuário: "endereço" = logradouro + bairro + cidade + UF (número, complemento e
**CEP** opcionais); o telefone obrigatório é o **celular**; a **descrição virou opcional**.

- `ValidarRequest` passou a exigir celular e deixou de exigir descrição.
- `CriarOcorrenciaRequest.DescricaoProblema` virou `string?`. A coluna é **NOT NULL**, então
  descrição ausente é gravada como **texto vazio** — sem migration.
- Front: `cep` usa `validarDigitosOpcional(8)`, `numero` e `descricaoProblema` sem `required`.
  CEP e descrição em branco vão como **null**, não `""`: os dois têm restrição de tamanho no
  contrato e string vazia reprovaria.

⚠️ **DataAnnotations do `CriarOcorrenciaRequest` não são validados.** Esse DTO chega como JSON
dentro do campo `Dados` do multipart e é desserializado **à mão** no controller — o ModelState
não roda. Valem só para o Swagger; quem reprova é `ValidarRequest`. Por isso o mínimo de 10
caracteres da descrição é cobrado **só pelo formulário** (e pela API na edição, que é
`[FromBody]` de verdade). Descoberto por ensaio: o build passava e a tela parecia certa.

### 2. Ficha de vistoria baixável em qualquer fase
O template **já existia** no servidor (`/arquivos/templates/Ficha de vistoria - Modelo
Oficial.docx`, o "REGISTRO DE OCORRÊNCIA"), com **31 marcadores**. Só faltava gerar.

- `RelatorioService.GerarFichaVistoriaAsync(ocorrenciaId)` devolve `byte[]` e **não grava nada**:
  sem storage, sem linha em `arquivos`. Campos que ainda não existem saem em branco.
- `GET /api/v1/ocorrencias/{id}/ficha-vistoria` → o .docx direto.
- Botão na **Central de Documentos** (`admin/ocorrencias/:id/documentos`), fora do fluxo de
  etapas justamente para estar ao alcance em qualquer fase.
- Antes da vistoria, `<<DATA_VISTORIA>>` usa a data **agendada** — é ela que a equipe leva impressa.
- ⚠️ O template escreve `<<HORARO_TENTATIVA_1..3>>` (sem o I). O código repete o erro de
  propósito: a tag tem de bater com o arquivo.
- `appsettings.json` ganhou `TemplateSettings:Templates:FichaVistoria`.

### 3. Eventos na agenda (indisponibilidade da equipe)
Decisões do usuário: **avisa e deixa seguir** (não bloqueia) e **intervalo de datas**.

- Tabela nova `eventos_agenda` (migration `20261006182027_EventosAgenda`): título, observação,
  `DataInicio`/`DataFim`, `Periodo` (MANHA/TARDE/DIA_TODO, texto), quem criou.
  **Só cria tabela** — não altera coluna, então não esbarra nas views de BI.
- `GET/POST/PUT/DELETE /api/v1/agenda/eventos`. A listagem usa sobreposição de intervalos
  (`DataInicio <= fim && DataFim >= inicio`), para férias que atravessam a semana aparecerem.
- Tela: botão "Evento" no cabeçalho e um "+" por turno (abre já com dia e período preenchidos).
  Evento de dia inteiro vira faixa sob o cabeçalho do dia; evento de turno fica dentro do bloco.
  Turno ocupado recebe listras diagonais. Clicar na faixa abre para editar/excluir.
- Soltar um card num período ocupado abre "Período ocupado — Há *X* neste período. Agendar mesmo
  assim?". Cancelar mantém o card no lugar; confirmar move normalmente.
- O contêiner `.eventos-dia` existe **mesmo vazio** (`min-height`): sem ele, os dias sem evento
  sobem e as colunas da semana deixam de se alinhar.
- Escopo: o evento vale para a **equipe toda**. Ausência de um vistoriador específico seria outro
  desenho e não foi feita.

⚠️ **Lacuna conhecida:** o aviso existe só no arrastar-e-soltar da agenda. Agendar pela tela de
detalhe da ocorrência (etapa 3) **não avisa** sobre evento no período.

### Validação
Ensaios de ponta a ponta contra a API real (PostgreSQL descartável na 55432):
**26 verificações** para obrigatórios + ficha (`scratchpad/testar_obrigatorios_e_ficha.py`) e
**22** para os eventos (`scratchpad/testar_eventos_agenda.py`). A agenda foi conferida também na
tela, com os dois caminhos do aviso.

⚠️ Lembrete que custou um ciclo: depois de `dotnet ef migrations add`, **rodar `dotnet build`**
antes de subir a API, senão ela carrega a DLL antiga e loga "No migrations were applied".

**Em produção desde 06/10/2026:** backend `c4db0b8`, frontend `f1da2b2`
(bundle `main-I6TPAV3P.js`), APK **1.4** (`versionCode 5`, mesma chave). Backups anteriores:
`~/backups/antes_agenda_eventos_20261006_1850.dump` e `~/backups/www_antes_agenda_20261006_1851.tgz`.

A migration `EventosAgenda` subiu limpa (confirmada no `__EFMigrationsHistory`), as 16 views de BI
voltaram e as **1241** ocorrências ficaram intactas. Conferido em produção: bundle novo servido,
`/agenda/eventos` pedindo token (401) e a ficha de uma ocorrência real gerando .docx de 566 KB
sem nenhum marcador por substituir.

---

## 🪪 CPF e e-mail opcionais na ocorrência (05/10/2026)

Pedido do usuário: boa parte dos atendimentos chega por telefone ou balcão, sem documento em mãos
e sem e-mail — exigir os dois travava a abertura.

⚠️ **Tirar o `[Required]` do DTO não basta.** Havia uma **segunda validação manual** em
`OcorrenciaController.ValidarRequest`, que devolvia 400 `VALIDACAO_FALHOU` mesmo com o DTO liberado.
Foi um ensaio de ponta a ponta contra a API real que revelou isso: o build passava e a tela liberava
o botão, mas a API recusava.

- `CidadaoDto`: `Cpf` e `Email` viraram `string?` sem `[Required]`.
- `ValidarRequest`: a exigência dos dois saiu; o **formato** do CPF (11 dígitos) continua cobrado
  quando o campo vem preenchido.
- `RelatorioService`: `<<EMAIL>>` era o único placeholder sem `?? string.Empty` e cai num
  `Replace(tag, value)` — sem a correção, gerar relatório de ocorrência sem e-mail lançaria
  `ArgumentNullException`. Já era risco latente nos registros históricos importados.
- Frontend: `nova.page` usa `validarDigitosOpcional(11)` no CPF e só `Validators.email` no e-mail;
  `detalhe.page` idem no formulário de edição. Campo vazio vai como **null**, não string vazia,
  para o banco guardar `NULL` como nos registros históricos.
- `cidadaoDto.ts` (contrato gerado do OpenAPI) ajustado à mão — não há script de geração no
  `package.json`.

**Sem migration:** as colunas já eram nulas. Na base de produção, 889 das 1236 ocorrências têm CPF
e apenas **225 têm e-mail** — a obrigatoriedade já não correspondia aos dados reais.

**Em produção desde 05/10/2026:** backend `d0b18c2`, frontend `4cbf309` (bundle `main-XPLF2BNO.js`),
APK **1.3** (`versionCode 4`). Backups anteriores ao deploy:
`~/backups/antes_cpf_email_20261005_1743.dump` e `~/backups/www_antes_cpf_email_20261005_1745.tgz`.

⚠️ **O IP externo parou de aceitar a chave SSH** (`179.106.96.58` → *Permission denied (publickey)*),
embora a porta 22 responda e o app siga no ar em 8081. O IP **interno** `192.168.8.15` continua
aceitando a mesma chave — este deploy foi todo por ele. Só funciona de dentro da rede da prefeitura.

### Verificação em produção sem criar registro
`ValidarRequest` roda **antes** da checagem de arquivos e antes de qualquer gravação. Mandando
`Dados` + `Comprovante` e **omitindo as Fotos**, a requisição morre em `ARQUIVOS_AUSENTES` sem
persistir nada — e o erro que volta diz qual código está no ar:
- sem CPF e sem e-mail → `ARQUIVOS_AUSENTES` ("ao menos uma foto") = passou pela validação ✅
- CPF com 3 dígitos → `VALIDACAO_FALHOU` ("CPF deve conter 11 dígitos") = formato ainda cobrado ✅

Script em `scratchpad/sondar_producao.py`. Atenção: o binder recusa antes do controller se o
`Comprovante` não vier (`IFormFile` não-nulo), então a sonda precisa mandá-lo.

### Pendência encontrada de passagem
O formulário de edição envia o CPF, mas `OcorrenciaService.AtualizarEtapa1Async` **não atualiza
`Cpf`** (atualiza nome, e-mail, telefone, celular, RG e órgão emissor). Corrigir um CPF digitado
errado na abertura não funciona hoje. É anterior a esta mudança e não foi mexido.

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
