# SIG-Defesa Civil — contexto do projeto

Sistema de gestão de vistorias de risco da Defesa Civil de Sabará/MG (TCC, IFMG).
Substitui um processo manual em papel/planilha por um fluxo digital web/móvel
com funcionamento **offline-first** para vistorias em campo.

## Repositórios / estrutura

Dois repositórios, em pastas irmãs:

- **Backend (esta pasta):** `SIG-Defesa-Civil.API` — GitHub `luciobeckler/SIG-Defesa-Civil.API-Claude`.
  Contém também o deploy: `docker-compose.yml`, `Dockerfile`, `nginx.conf`.
- **Frontend:** `../sig-defesa-civil-frontend` — GitHub `luciobeckler/sig-defesa-civil-frontend`.

## Tecnologias

- **Backend:** ASP.NET Core 8 (.NET 8), EF Core + PostgreSQL (Npgsql).
- **Frontend:** Angular 20 + Ionic 8, componentes standalone, Signals.
- **Deploy:** Docker Compose (postgres 16 + api + nginx) em servidor on-premises da prefeitura.

## Como rodar localmente

```bash
# Backend → https://localhost:7180 (+ http://localhost:5218)
cd SIG-Defesa-Civil.API
dotnet run --launch-profile https

# Frontend → http://localhost:4200 (proxy.conf.json encaminha /api → https://localhost:7180)
cd ../sig-defesa-civil-frontend
npm start
```

- Connection string de dev e segredos ficam em **User Secrets** (`dotnet user-secrets`), não no repo.
  Banco local: `Host=localhost;Port=5432;Database=sig-defesa-civil;Username=postgres`.
- As migrations são **aplicadas automaticamente no startup** (`MigrateAsync` em `Infrastructure/Seeders/AdminSeeder.cs`), que também semeia o admin a partir de `AdminSeed__*`.

## Comandos de validação

- **Compilar backend sem travar no .exe** (quando a API está rodando): `dotnet build --nologo -v q -t:CoreCompile`.
- **Build do frontend:** `npx ng build --configuration development`.
- **Migrations:** a API trava a DLL — **pare a API antes**. Depois: `dotnet build`, `dotnet ef migrations add <Nome> --no-build`, `dotnet ef database update --no-build`.
  Atenção: `--no-build` usa a DLL já compilada; sempre `dotnet build` **após** criar a migration antes de aplicar.

## Convenções e decisões de modelo (importantes)

- **Campos de seleção da vistoria são TEXTO, não enums** (edificação, estrutura, grau, tipo de risco, áreas afetadas, motivação, orientações, interdição, remoção, caracterização). Isso permite **opções personalizadas do catálogo** (`OpcaoCampoVistoria` / `opcoes_campo_vistoria`), criadas em runtime pelos usuários. Enums antigos desses campos foram removidos; `GrauRisco`, `TipificacaoOcorrencia` e `Encaminhamento` permanecem (usados fora da vistoria).
- **Cidadãos NÃO são usuários.** A abertura é um endpoint público e o solicitante não tem conta.
  Os dados dele ficam em colunas `Solicitante*` da própria tabela `ocorrencias`, mapeadas como
  **owned entity** do EF (`SolicitanteOcorrencia`) — por isso o código lê `ocorrencia.Solicitante.Nome`
  mas **não** existe `Include(o => o.Solicitante)` (owned carrega junto; `Include` quebra).
  Como consequência, `ocorrencias.CriadoPorId`, `arquivos.enviado_por` e `log_acesso_lgpd.UsuarioId`
  são **nullable** (nulo = veio do portal público). Nunca recriar contas `CIDADAO`.
  Os dados são um retrato da abertura: editar uma ocorrência não altera as demais do mesmo CPF.
- **Tipificação é multivalorada nas duas etapas.** `AvaliacaoRisco.TipificacaoInicial` e
  `Vistoria.TipificacaoOcorrencia` são `List<string>` (`text[]`) — uma ocorrência costuma
  acumular mais de uma (trincas + infiltração). Texto, não enum, para aceitar as opções do catálogo.
- **Views de BI bloqueiam migrations.** O PostgreSQL recusa alterar o tipo de uma coluna lida por
  uma view. Por isso o `Program.cs` roda `ViewsBiSeeder.RemoverAsync` → migrations → `SeedAsync`.
  Migration aplicada à mão (`dotnet ef database update`) precisa derrubar as `vw_bi_*` ela mesma.
- **Protocolo** no formato `AAAA-NNNN` (ex.: `2026-0001`), gerado pela sequence `seq_protocolo_ano`.
- **Storage de arquivos:** uma pasta por categoria, espelhando a Central de Documentos — ver `Services/Storage/PastasArquivo.cs`. Vale para storage local e Cloudflare R2. Templates de relatório ficam em `/arquivos/templates` (fora do banco).
- **Notificados** = quem recebeu o relatório (propriedade da ocorrência, **não** uma etapa); podem ser registrados a qualquer momento e não alteram o status. Recebimento `EMAIL` ou `PRESENCIAL` (presencial exige assinatura do notificado).
- **Offline-first:** vistorias podem ser preenchidas sem rede (fila local + fotos no Filesystem + sincronização idempotente ao reconectar). A assinatura do munícipe é **obrigatória** para registrar a vistoria.
- Código, comentários e mensagens de commit em **português**.

## Fluxo da ocorrência (status)

`ABERTA → EM_AVALIACAO → VISTORIA_SOLICITADA → VISTORIA_REALIZADA → NOTIFICADA → ENCERRADA` (+ `CANCELADA`).
Etapas: 1) abertura, 2) avaliação de risco, 3) agendamento (calendário/agenda, equipe de **até 4** vistoriadores; só o 1º é obrigatório), 4) vistoria presencial (offline, assinatura obrigatória), relatório (gerado a partir de template + retorno do relatório **assinado em PDF**), notificados (propriedade), 6) encaminhamento final. A entrega do relatório é **sempre por e-mail** (não há canal configurável).

## ⚠️ Deploy de produção — pendência crítica

As migrations foram **achatadas** (o baseline `AlterandoBanco` recria todas as tabelas) e uma delas (`OpcoesPersonalizadasVistoria`) **converte colunas `integer[]→text[]`**, o que falha em tabela com dados. Por isso **um `docker compose up` direto contra o banco atual do servidor quebra**. Há duas saídas — recriar o banco (`down -v`, perde dados) ou uma migração preservando dados. Ver `HANDOFF.md` para o estado exato dessa decisão.
