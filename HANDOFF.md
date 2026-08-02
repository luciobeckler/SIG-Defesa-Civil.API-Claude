# HANDOFF — estado da sessão (28/07/2026)

Documento de passagem de contexto para continuar em uma nova sessão do Claude Code.
Leia junto com `CLAUDE.md` (arquitetura, como rodar, convenções). Datas em absoluto.

---

## ✅ Entregue (últimas ~2 semanas)

Tudo compilando (backend `dotnet build` 0 erros; frontend `ng build` OK) e validado.

- **Solicitante deixou de ser usuário (28/07/2026).** A abertura pública criava uma conta
  `CIDADAO` por CPF — 1.078 contas poluindo a tabela `usuarios`, e reabrir com o mesmo CPF
  reescrevia o nome/contato das ocorrências antigas. Agora os dados moram em colunas
  `Solicitante*` da própria `ocorrencias` (owned entity `SolicitanteOcorrencia`).
  `CriadoPorId`, `arquivos.enviado_por` e `log_acesso_lgpd.UsuarioId` viraram nullable.
  Migration `20260729002541_SolicitanteEmbutidoNaOcorrencia` copia os dados e apaga os `CIDADAO`.
  **Os DTOs não mudaram de forma — o frontend não precisou de alteração.**
  Validado: 4 ocorrências criadas pelo endpoint real sem criar nenhum usuário; listagem,
  detalhe, filtro por CPF, acompanhamento público e revelação LGPD conferidos.
  Backup pré-mudança: `TCC/backups/sig-defesa-civil_pre-solicitante_20260728.dump`.

- **Central de Documentos:** upload de arquivos por categoria + **criação de pastas** personalizadas
  (ex.: "Retorno"); pastas físicas no servidor espelham as categorias (`Services/Storage/PastasArquivo.cs`).
- **Retorno do relatório assinado (PDF):** nova seção no detalhe; download do relatório final no
  acompanhamento público do cidadão (protocolo + CPF).
- **Notificados** viraram propriedade da ocorrência (não etapa); recebimento EMAIL/PRESENCIAL,
  presencial exige **assinatura do notificado**. Campo "Canal de entrega do relatório" **removido**
  (entrega sempre por e-mail).
- **Equipe de vistoria de até 4 pessoas** (só o 1º obrigatório) — agendamento, vistoria, agenda/calendário.
- **Gestão de usuários:** ADMIN pode **editar nome/e-mail** (`PUT /usuarios/{id}`) e **desativar/reativar**
  (`PATCH /usuarios/{id}/ativo`) — desativação lógica, nunca deleção; protege autodesativação e último admin.
- **Listagem com Kanban + abas** (última entrega): abas **Ativas / Kanban / Histórico** e endpoint
  **`GET /api/v1/ocorrencias/resumo`** (`{ total, ativas, arquivo, porStatus }`). Resolve a lentidão da
  lista única com base grande. Filtro `situacao=ATIVAS|ARQUIVO` na listagem.
  Arquivos: `listagem.page.{ts,html,scss}`, `OcorrenciaController.ObterResumo`, `OcorrenciaService.ObterResumoAsync`,
  `FiltroOcorrenciaDto.Situacao`, `ResumoOcorrenciasDto`.
- **Importador da planilha histórica** (ver seção própria abaixo).

## 🧭 Migrations presentes (aplicadas no banco local)

```
20260616003434_AlterandoBanco                 (baseline achatado — cria todas as tabelas)
20260616022930_AdicionarDataAgendamento
20260623021449_OpcoesPersonalizadasVistoria   (integer[]→text[] nas colunas da vistoria)
20260710110926_EquipeVistoriaQuatroPessoas
20260714015752_NotificadosPropriedadeDaOcorrencia
20260729002541_SolicitanteEmbutidoNaOcorrencia  (solicitante embutido + DELETE dos CIDADAO)
```

## 🗄️ Estado do banco LOCAL agora

Carga da planilha **já aplicada e commitada localmente**: **1.112 ocorrências** (`2020-0132` a `2026-0678`),
1.034 ENCERRADA / 67 VISTORIA_REALIZADA / 11 VISTORIA_SOLICITADA. As 2 ocorrências de teste foram apagadas;
admin preservado (`admin@defesacivil.sabara.mg.gov.br`).

Após a mudança do solicitante (28/07): **1.116 ocorrências** (as 4 de teste `2026-0679`…`2026-0682`)
e **98 usuários** — 96 VISTORIADOR + 1 ADMIN + 1 ATENDENTE, **nenhum CIDADAO**.

---

## ⏳ Pendências

### 1. Deploy no servidor (ainda não feito)
- Acesso SSH **só pelo IP externo**: `ssh luciobeckler@179.106.96.58` (porta 22 aberta).
  O IP interno `192.168.8.15` está **inacessível** da rede atual do usuário. App externo: `http://179.106.96.58:8081/`.
- Home Apache citado pelo usuário: `/var/www/html` — **mas** o projeto sobe **nginx em contêiner** servindo `./www`.
  Confirmar no servidor onde o front deve ir (rodar `docker compose ps` + `ss -tlnp | grep -E ':80|:8081'`).
- **Backend:** `git pull` na pasta do projeto → `docker compose up -d --build` (compila o C# no Dockerfile).
- **Frontend:** repo NÃO está no servidor → buildar localmente (`npm run build`, gera `sig-defesa-civil-frontend/www/`)
  e enviar para `SIG-Defesa-Civil.API/www/` via `rsync`/`scp`. `www` não é versionado.

### 2. ⚠️ DECISÃO EM ABERTO — recriar o banco de produção × preservar dados
Um deploy direto **falha** no servidor por 2 motivos: (a) histórico de migrations achatado
(o EF tenta re-rodar `AlterandoBanco` e erra "tabela já existe"); (b) `OpcoesPersonalizadasVistoria`
converte `integer[]→text[]` — falha em tabela populada.
- **Caminho A (perde dados):** `docker compose down -v` + `up -d --build`. Simples.
- **Caminho B (preserva dados):** reconciliar `__EFMigrationsHistory` (marcar baseline como aplicado) +
  converter as 6 colunas de `vistorias` com `USING` (mapeando código numérico → nome do enum) +
  backfill de `FormaRecebimento`. **Falta o usuário rodar 3 diagnósticos no servidor** e colar a saída:
  1. `docker compose exec db psql -U defesacivil -d defesacivil -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;'`
  2. `docker compose exec db psql -U defesacivil -d defesacivil -c '\d vistorias'` (ver se colunas ainda são `integer[]`)
  3. contagens de `ocorrencias/vistorias/notificados`.
  Com isso, gerar um `migrar_servidor.sql` sob medida. (Risco: mapa de ordinais depende da ordem do enum no deploy do servidor — verificar no git.)

### 3. Importador da planilha
- Script: `Scripts/Importacao/importar_planilha.py`. Saída principal: `Scripts/Importacao/out/import.sql`.
  Cópias entregues em `C:\Users\lucio\Desktop\TCC\Scripts\`.
- Gera protocolos `AAAA-NNNN`, tem **guarda de colisão** (aborta se protocolo já existir e não for da importação),
  **avança a sequence** `seq_protocolo_ano`, e flag **`--limpar`** (apaga ocorrências + catálogo, **preserva usuários**).
  Rodar: `python importar_planilha.py "<planilha.xlsx>" --limpar`.
- Idempotente (rodar 2× não duplica). Fonte atual: `C:\Users\lucio\Downloads\PLANILHAS DE OCORRENCIAS (1).xlsx`.
- **De-para de vistoriadores** editável: `Scripts/Importacao/mapeamento_vistoriadores.csv`
  (canônico em branco = ignorar, ex.: "DEMAIS SECRETARIAS"). Decisão pendente do usuário: unificar ou não
  variações como `Leandro`/`Leandro de Jesus`/`Leandro Santos` e `Paulo`/`Paulo R`/`Paulo Rogerio`.
- Passo a passo no servidor: `scp` do `import.sql` → `pg_dump` de backup → `psql -v ON_ERROR_STOP=1 < import.sql` → conferir.

### 4. Artigo SBC (TCC)
- Esqueleto `.tex` compilável (formato SBC) em
  `C:\Users\lucio\Downloads\SIG_Defesa_Civil___Lýcio_Beckler_Passos\artigo-sig-defesa-civil.tex`.
  Estrutura: relato de experiência (as-is → to-be), foco em processos. Tem placeholders de figura
  (`figs/*.png`), tabelas e marcadores `% [COLETAR: ...]` para os dados de campo (tempos antes/depois, SUS, referências).

### 5. Pequeno TODO deixado em aberto
- No **modo offline** da listagem, os badges das abas ficam ocultos (o `/resumo` não é buscado sem rede).
  Sugerido calcular os contadores a partir do cache local — **não implementado**.

---

## Serviços locais
Não sobrevivem a novas sessões. Para visualizar de novo: `dotnet run --launch-profile https` (API) e
`npm start` (front, http://localhost:4200). Login: `admin@defesacivil.sabara.mg.gov.br`.
