-- ════════════════════════════════════════════════════════════════════════════
--  Camada de BI do SIG-Defesa Civil — views de leitura
-- ════════════════════════════════════════════════════════════════════════════
--
--  FONTE ÚNICA DA VERDADE. Este arquivo é aplicado automaticamente na
--  inicialização da API (Infrastructure/Seeders/ViewsBiSeeder.cs) e pode ser
--  rodado à mão a qualquer momento — é idempotente (CREATE OR REPLACE).
--
--  Para que serve: isolar o Power BI (e um futuro painel nativo) do schema
--  interno. As regras de negócio do BI — o que é "relatório pendente", como se
--  mede o tempo de atendimento, o que conta como risco alto — moram aqui, em um
--  lugar só, versionadas no repositório.
--
--  LGPD: nenhuma view expõe dado pessoal do solicitante (nome, CPF, RG, e-mail,
--  telefone). O BI trabalha com protocolo, território, datas e classificações.
--  Se algum dia for preciso identificar o cidadão, isso passa pelo endpoint de
--  revelação da API, que registra o acesso — não por aqui.
--
--  SOBRE OS CAMPOS AINDA VAZIOS: várias views abaixo (moradores por imóvel,
--  remoção, regime de ocupação, encaminhamentos, notificados, avaliação de
--  risco) hoje retornam pouco ou nada, porque a carga histórica da planilha não
--  trazia esses campos. Elas estão prontas e passam a produzir resultado
--  sozinhas conforme o sistema for usado. Nenhuma delas quebra com tabela vazia
--  — retornam zero linhas.
--
--  Exceção: o tipo de risco JÁ tem dado. A coluna TipoRisco está vazia, mas o
--  multivalorado TipificacaoOcorrencia veio preenchido na importação (1.085
--  valores). Os textos são livres e sujos — por isso o agrupamento é sempre
--  pela chave normalizada, nunca pela grafia crua.
--
--  Convenção: prefixo vw_bi_ para views, fn_bi_ para funções auxiliares.
--  Soft-delete: ocorrências com DeletedAt preenchido ficam FORA de todas as views.
-- ════════════════════════════════════════════════════════════════════════════


-- ────────────────────────────────────────────────────────────────────────────
--  FUNÇÕES AUXILIARES
-- ────────────────────────────────────────────────────────────────────────────

-- Normaliza texto para agrupamento: sem acento, maiúsculas, espaços colapsados.
-- Usa translate() em vez da extensão unaccent, que exige superusuário e não está
-- instalada no banco de produção. Sem isso, "Nossa Senhora de Fátima" e
-- "Nossa Senhora DE Fatima" viram dois bairros diferentes — como de fato estavam.
CREATE OR REPLACE FUNCTION fn_bi_normaliza(txt text)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
AS $$
    SELECT NULLIF(
        regexp_replace(
            upper(translate(
                btrim(coalesce(txt, '')),
                'áàâãäéèêëíìîïóòôõöúùûüçñÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇÑ',
                'aaaaaeeeeiiiiooooouuuucnAAAAAEEEEIIIIOOOOOUUUUCN'
            )),
            '\s+', ' ', 'g'
        ),
        ''
    );
$$;

-- Converte os marcadores de ausência da importação em NULL de verdade, para que
-- "Não informado" não seja contado como se fosse uma categoria real.
-- Atenção: "Não constatado" NÃO entra aqui — é um resultado legítimo de vistoria
-- (o vistoriador foi ao local e não constatou risco).
CREATE OR REPLACE FUNCTION fn_bi_valor(txt text)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
AS $$
    SELECT CASE
        WHEN fn_bi_normaliza(txt) IS NULL THEN NULL
        WHEN fn_bi_normaliza(txt) IN (
            'NAO INFORMADO', 'NAO INFORMADA', 'NAO INFORMADOS',
            'N/A', 'NA', '-', 'SEM INFORMACAO'
        ) THEN NULL
        ELSE btrim(txt)
    END;
$$;

-- Enum Encaminhamento (Enums/Enums.cs) gravado como ordinal em integer[].
CREATE OR REPLACE FUNCTION fn_bi_encaminhamento(codigo integer)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
AS $$
    SELECT CASE codigo
        WHEN 0 THEN 'Ajuda humanitária'
        WHEN 1 THEN 'CEMIG'
        WHEN 2 THEN 'COPASA'
        WHEN 3 THEN 'DNIT'
        WHEN 4 THEN 'Outros'
        WHEN 5 THEN 'Providências pelo morador'
        WHEN 6 THEN 'Sec. Desenvolvimento Social'
        WHEN 7 THEN 'Sec. Meio Ambiente'
        WHEN 8 THEN 'Sec. Obras'
        ELSE 'Código ' || codigo::text
    END;
$$;

-- Ordem das etapas do fluxo, para o funil sair na sequência certa no Power BI
-- (que ordena alfabeticamente por padrão e embaralharia as etapas).
CREATE OR REPLACE FUNCTION fn_bi_ordem_status(status text)
RETURNS integer
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
AS $$
    SELECT CASE status
        WHEN 'ABERTA'              THEN 1
        WHEN 'EM_AVALIACAO'        THEN 2
        WHEN 'VISTORIA_SOLICITADA' THEN 3
        WHEN 'VISTORIA_REALIZADA'  THEN 4
        WHEN 'NOTIFICADA'          THEN 5
        WHEN 'ENCERRADA'           THEN 6
        WHEN 'CANCELADA'           THEN 7
        ELSE 99
    END;
$$;

-- Faixa de envelhecimento, usada tanto na fila operacional quanto no semáforo.
CREATE OR REPLACE FUNCTION fn_bi_faixa_aging(dias integer)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
AS $$
    SELECT CASE
        WHEN dias IS NULL THEN NULL
        WHEN dias <= 30  THEN 'Em dia'
        WHEN dias <= 90  THEN 'Atenção'
        WHEN dias <= 365 THEN 'Atrasado'
        ELSE 'Crítico'
    END;
$$;

-- Faixas de tempo de atendimento (abertura → vistoria).
CREATE OR REPLACE FUNCTION fn_bi_faixa_tempo(dias integer)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
AS $$
    SELECT CASE
        WHEN dias IS NULL THEN NULL
        WHEN dias <= 3  THEN '0-3 dias'
        WHEN dias <= 7  THEN '4-7 dias'
        WHEN dias <= 15 THEN '8-15 dias'
        WHEN dias <= 30 THEN '16-30 dias'
        ELSE '31+ dias'
    END;
$$;


-- ════════════════════════════════════════════════════════════════════════════
--  1. FATO PRINCIPAL — uma linha por ocorrência
-- ════════════════════════════════════════════════════════════════════════════
--  É a tabela que o Power BI deve carregar primeiro. Quase todo indicador do
--  painel sai daqui sem precisar de join.
CREATE OR REPLACE VIEW vw_bi_ocorrencias AS
WITH ultima_vistoria AS (
    SELECT DISTINCT ON (v."OcorrenciaId") v.*
    FROM vistorias v
    ORDER BY v."OcorrenciaId", v."Numero" DESC, v."Id" DESC
),
ultimo_agendamento AS (
    SELECT DISTINCT ON (a."OcorrenciaId") a.*
    FROM agendamentos_vistoria a
    ORDER BY a."OcorrenciaId", a."Numero" DESC, a."Id" DESC
),
relatorios AS (
    SELECT
        ocorrencia_id,
        min(enviado_em) FILTER (WHERE tipo_arquivo = 'RELATORIO_FINAL')    AS data_relatorio_final,
        min(enviado_em) FILTER (WHERE tipo_arquivo = 'RELATORIO_ASSINADO') AS data_relatorio_assinado,
        count(*)        FILTER (WHERE tipo_arquivo = 'FOTO_CAMPO')         AS qtd_fotos_campo,
        count(*)                                                           AS qtd_arquivos
    FROM arquivos
    GROUP BY ocorrencia_id
),
notificacoes AS (
    SELECT "OcorrenciaId" AS ocorrencia_id,
           count(*)          AS qtd_notificados,
           min("DataNotificacao") AS data_primeira_notificacao
    FROM notificados
    GROUP BY "OcorrenciaId"
)
SELECT
    -- ── Identificação ───────────────────────────────────────────────────────
    o."Id"                                              AS ocorrencia_id,
    o."Protocolo"                                       AS protocolo,

    -- ── Tempo de abertura ───────────────────────────────────────────────────
    o."AbertaEm"                                        AS aberta_em,
    o."AbertaEm"::date                                  AS data_abertura,
    date_trunc('month', o."AbertaEm")::date             AS ano_mes,
    extract(year  FROM o."AbertaEm")::int               AS ano,
    extract(month FROM o."AbertaEm")::int               AS mes,
    to_char(o."AbertaEm", 'YYYY-MM')                    AS ano_mes_rotulo,
    -- Período chuvoso em Sabará/MG: outubro a março. É o recorte que dimensiona
    -- escala de plantão — 73% da demanda histórica cai nesta janela.
    (extract(month FROM o."AbertaEm")::int IN (10, 11, 12, 1, 2, 3)) AS periodo_chuvoso,
    o."AtualizadoEm"                                    AS atualizado_em,

    -- ── Situação ────────────────────────────────────────────────────────────
    o."Status"                                          AS status,
    fn_bi_ordem_status(o."Status")                      AS status_ordem,
    CASE WHEN o."Status" IN ('ENCERRADA', 'CANCELADA')
         THEN 'Arquivo' ELSE 'Ativa' END                AS situacao,
    (o."Status" NOT IN ('ENCERRADA', 'CANCELADA'))      AS em_aberto,

    -- ── Território ──────────────────────────────────────────────────────────
    fn_bi_valor(l."Bairro")                             AS bairro,
    fn_bi_normaliza(fn_bi_valor(l."Bairro"))            AS bairro_chave,
    fn_bi_valor(l."Cidade")                             AS cidade,
    fn_bi_valor(l."Uf")                                 AS uf,
    fn_bi_valor(l."Cep")                                AS cep,
    (l."Coordenada" IS NOT NULL AND btrim(l."Coordenada") <> '') AS tem_coordenada,
    l."Coordenada"                                      AS coordenada,

    -- ── Etapa 2: avaliação de risco (hoje sem registros) ────────────────────
    (ar."Id" IS NOT NULL)                               AS tem_avaliacao_risco,
    fn_bi_valor(ar."GrauRiscoInicial")                  AS grau_risco_inicial,
    -- Multivalorada (text[]): vai como texto legível para os cartões e como
    -- array para quem quiser explodir em linhas.
    nullif(array_to_string(ar."TipificacaoInicial", '; '), '') AS tipificacao_inicial,
    ar."TipificacaoInicial"                             AS tipificacao_inicial_lista,
    coalesce(array_length(ar."TipificacaoInicial", 1), 0) AS qtd_tipificacoes_iniciais,
    ar."Emergencia"                                     AS emergencia,
    ar."RegistradoEm"                                   AS avaliacao_registrada_em,

    -- ── Etapa 3: agendamento ────────────────────────────────────────────────
    ag."Data"                                           AS data_agendada,
    fn_bi_valor(ag."Status")                            AS status_agendamento,
    CASE ag."Turno" WHEN 0 THEN 'Manhã' WHEN 1 THEN 'Tarde' END AS turno_agendado,

    -- ── Etapa 4: vistoria (última realizada) ────────────────────────────────
    (uv."Id" IS NOT NULL)                               AS tem_vistoria,
    uv."DataVistoria"                                   AS data_vistoria,
    uv."Numero"                                         AS numero_vistorias,
    fn_bi_valor(uv."GrauRiscoEncontrado")               AS grau_risco_encontrado,
    fn_bi_valor(uv."TipoRisco")                         AS tipo_risco,
    fn_bi_valor(uv."Interdicao")                        AS interdicao,
    fn_bi_valor(uv."Remocao")                           AS remocao,
    fn_bi_valor(uv."RegimeOcupacao")                    AS regime_ocupacao,
    fn_bi_valor(uv."Edificacao")                        AS edificacao,
    fn_bi_valor(uv."Estrutura")                         AS estrutura,
    fn_bi_valor(uv."CaracterizacaoDoLocal")             AS caracterizacao_local,
    -- Risco alto é o recorte de prioridade da coordenação
    (fn_bi_normaliza(uv."GrauRiscoEncontrado") IN ('ALTO', 'MUITO ALTO', 'MUITO_ALTO')) AS risco_alto,
    (fn_bi_normaliza(uv."Interdicao") IN ('TOTAL', 'PARCIAL'))                          AS houve_interdicao,

    -- ── População exposta (hoje zerada na carga histórica) ──────────────────
    nullif(uv."TotalMoradores", 0)                      AS total_moradores,
    nullif(uv."NumeroAdultos", 0)                       AS adultos,
    nullif(uv."NumeroCriancas", 0)                      AS criancas,
    nullif(uv."NumeroIdosos", 0)                        AS idosos,
    nullif(uv."NumeroDeficientes", 0)                   AS pessoas_com_deficiencia,
    nullif(uv."NumeroMoradias", 0)                      AS numero_moradias,
    nullif(uv."NumeroComodos", 0)                       AS numero_comodos,
    nullif(uv."NumeroPavimentos", 0)                    AS numero_pavimentos,
    uv."PossuiUnidadeFamiliar"                          AS possui_unidade_familiar,
    -- Grupos que exigem prioridade em remoção e abrigamento
    (coalesce(uv."NumeroCriancas", 0) + coalesce(uv."NumeroIdosos", 0)
        + coalesce(uv."NumeroDeficientes", 0))          AS pessoas_vulneraveis,

    -- ── Relatório e encerramento ────────────────────────────────────────────
    rel.data_relatorio_final                            AS data_relatorio_final,
    rel.data_relatorio_assinado                         AS data_relatorio_assinado,
    (ef."Id" IS NOT NULL)                               AS tem_encaminhamento_final,
    ef."RegistradoEm"                                   AS encerrada_em,
    -- Reproduz o "Status Relatório" da planilha, que o modelo não guardava como
    -- campo. Derivado em um lugar só, para o painel e o Power BI não divergirem.
    -- O status ENCERRADA conta como concluído: a carga histórica encerrou 1.033
    -- ocorrências sem criar encaminhamento final, e sem esta regra todas elas
    -- apareceriam como relatório pendente.
    CASE
        WHEN rel.data_relatorio_assinado IS NOT NULL THEN 'Concluído'
        WHEN ef."Id" IS NOT NULL                     THEN 'Concluído'
        WHEN o."Status" = 'ENCERRADA'                THEN 'Concluído'
        WHEN o."Status" = 'CANCELADA'                THEN 'Não aplicável'
        WHEN uv."Id" IS NOT NULL                     THEN 'Pendente'
        ELSE 'Não aplicável'
    END                                                 AS situacao_relatorio,

    -- ── Notificados ─────────────────────────────────────────────────────────
    coalesce(nt.qtd_notificados, 0)                     AS qtd_notificados,
    nt.data_primeira_notificacao                        AS data_primeira_notificacao,

    -- ── Arquivos ────────────────────────────────────────────────────────────
    coalesce(rel.qtd_arquivos, 0)                       AS qtd_arquivos,
    coalesce(rel.qtd_fotos_campo, 0)                    AS qtd_fotos_campo,

    -- ── Tempos (o coração dos indicadores de desempenho) ────────────────────
    (uv."DataVistoria" - o."AbertaEm"::date)            AS dias_ate_vistoria_bruto,
    -- A carga histórica tem 31 registros com data impossível (um deles no ano
    -- 205). Sem esse filtro a média vira -600 dias. O campo bruto fica
    -- disponível ao lado para auditoria.
    CASE WHEN (uv."DataVistoria" - o."AbertaEm"::date) BETWEEN 0 AND 365
         THEN (uv."DataVistoria" - o."AbertaEm"::date) END AS dias_ate_vistoria,
    fn_bi_faixa_tempo(
        CASE WHEN (uv."DataVistoria" - o."AbertaEm"::date) BETWEEN 0 AND 365
             THEN (uv."DataVistoria" - o."AbertaEm"::date) END
    )                                                   AS faixa_tempo_vistoria,
    (rel.data_relatorio_final::date - uv."DataVistoria") AS dias_vistoria_ate_relatorio,
    (ef."RegistradoEm"::date - o."AbertaEm"::date)       AS dias_ate_encerramento,
    CASE WHEN o."Status" NOT IN ('ENCERRADA', 'CANCELADA')
         THEN (CURRENT_DATE - o."AbertaEm"::date) END    AS dias_em_aberto,
    fn_bi_faixa_aging(
        CASE WHEN o."Status" NOT IN ('ENCERRADA', 'CANCELADA')
             THEN (CURRENT_DATE - o."AbertaEm"::date) END
    )                                                   AS faixa_aging,

    -- ── Origem ──────────────────────────────────────────────────────────────
    (o."CriadoPorId" IS NULL)                           AS aberta_pelo_portal

FROM ocorrencias o
LEFT JOIN localizacoes        l   ON l."OcorrenciaId"  = o."Id"
LEFT JOIN avaliacoes_risco    ar  ON ar."OcorrenciaId" = o."Id"
LEFT JOIN ultimo_agendamento  ag  ON ag."OcorrenciaId" = o."Id"
LEFT JOIN ultima_vistoria     uv  ON uv."OcorrenciaId" = o."Id"
LEFT JOIN encaminhamentos_finais ef ON ef."OcorrenciaId" = o."Id"
LEFT JOIN relatorios          rel ON rel.ocorrencia_id  = o."Id"
LEFT JOIN notificacoes        nt  ON nt.ocorrencia_id   = o."Id"
WHERE o."DeletedAt" IS NULL;

COMMENT ON VIEW vw_bi_ocorrencias IS
    'Fato principal do BI: uma linha por ocorrência não excluída, com território, '
    'classificação de risco, população exposta, tempos e situação do relatório. '
    'Sem dado pessoal do solicitante (LGPD).';


-- ════════════════════════════════════════════════════════════════════════════
--  2. FATO DE VISTORIAS — uma linha por vistoria realizada
-- ════════════════════════════════════════════════════════════════════════════
--  Necessário porque uma ocorrência pode ter mais de uma vistoria (revisita).
--  A vw_bi_ocorrencias só traz a última.
CREATE OR REPLACE VIEW vw_bi_vistorias AS
SELECT
    v."Id"                                    AS vistoria_id,
    v."OcorrenciaId"                          AS ocorrencia_id,
    o."Protocolo"                             AS protocolo,
    v."Numero"                                AS numero,
    v."DataVistoria"                          AS data_vistoria,
    date_trunc('month', v."DataVistoria")::date AS ano_mes_vistoria,
    extract(year  FROM v."DataVistoria")::int AS ano_vistoria,
    extract(month FROM v."DataVistoria")::int AS mes_vistoria,
    v."HorarioInicio"                         AS horario_inicio,
    v."HorarioTermino"                        AS horario_termino,
    -- Duração da visita: insumo para dimensionar quantas vistorias cabem no dia
    CASE WHEN v."HorarioTermino" > v."HorarioInicio"
         THEN extract(epoch FROM (v."HorarioTermino" - v."HorarioInicio")) / 60.0
    END                                       AS duracao_minutos,

    fn_bi_valor(l."Bairro")                   AS bairro,
    fn_bi_normaliza(fn_bi_valor(l."Bairro"))  AS bairro_chave,

    fn_bi_valor(v."GrauRiscoEncontrado")      AS grau_risco_encontrado,
    fn_bi_valor(v."TipoRisco")                AS tipo_risco,
    fn_bi_valor(v."Interdicao")               AS interdicao,
    fn_bi_valor(v."Remocao")                  AS remocao,
    fn_bi_valor(v."RegimeOcupacao")           AS regime_ocupacao,
    fn_bi_valor(v."Edificacao")               AS edificacao,
    fn_bi_valor(v."Estrutura")                AS estrutura,
    fn_bi_valor(v."CaracterizacaoDoLocal")    AS caracterizacao_local,
    (fn_bi_normaliza(v."GrauRiscoEncontrado") IN ('ALTO', 'MUITO ALTO', 'MUITO_ALTO')) AS risco_alto,
    (fn_bi_normaliza(v."Interdicao") IN ('TOTAL', 'PARCIAL'))                          AS houve_interdicao,

    nullif(v."TotalMoradores", 0)             AS total_moradores,
    nullif(v."NumeroAdultos", 0)              AS adultos,
    nullif(v."NumeroCriancas", 0)             AS criancas,
    nullif(v."NumeroIdosos", 0)               AS idosos,
    nullif(v."NumeroDeficientes", 0)          AS pessoas_com_deficiencia,
    (coalesce(v."NumeroCriancas", 0) + coalesce(v."NumeroIdosos", 0)
        + coalesce(v."NumeroDeficientes", 0)) AS pessoas_vulneraveis,
    nullif(v."NumeroMoradias", 0)             AS numero_moradias,
    nullif(v."NumeroMoradiasNoLote", 0)       AS moradias_no_lote,
    nullif(v."NumeroComodos", 0)              AS numero_comodos,
    nullif(v."NumeroPavimentos", 0)           AS numero_pavimentos,
    v."PossuiUnidadeFamiliar"                 AS possui_unidade_familiar,

    v."Vistoriador1Id"                        AS vistoriador1_id,
    v."Vistoriador2Id"                        AS vistoriador2_id,
    v."Vistoriador3Id"                        AS vistoriador3_id,
    v."Vistoriador4Id"                        AS vistoriador4_id,
    (1 + (v."Vistoriador2Id" IS NOT NULL)::int
       + (v."Vistoriador3Id" IS NOT NULL)::int
       + (v."Vistoriador4Id" IS NOT NULL)::int) AS tamanho_equipe,

    v."RegistradoEm"                          AS registrada_em,
    (v."DataVistoria" - o."AbertaEm"::date)   AS dias_ate_vistoria_bruto,
    CASE WHEN (v."DataVistoria" - o."AbertaEm"::date) BETWEEN 0 AND 365
         THEN (v."DataVistoria" - o."AbertaEm"::date) END AS dias_ate_vistoria
FROM vistorias v
JOIN ocorrencias o ON o."Id" = v."OcorrenciaId"
LEFT JOIN localizacoes l ON l."OcorrenciaId" = o."Id"
WHERE o."DeletedAt" IS NULL;

COMMENT ON VIEW vw_bi_vistorias IS
    'Uma linha por vistoria realizada, incluindo revisitas. Traz composição '
    'familiar, duração da visita e tamanho da equipe.';


-- ════════════════════════════════════════════════════════════════════════════
--  3. CAMPOS MULTIVALORADOS DA VISTORIA
-- ════════════════════════════════════════════════════════════════════════════
--  Tipificação, motivação, áreas afetadas, orientações e encaminhamentos de
--  campo são text[] (multi-seleção). Uma view única com a coluna "campo" evita
--  cinco views quase idênticas — no Power BI, basta filtrar por campo.
--  Hoje retorna pouco: a planilha histórica não trazia esses dados.
CREATE OR REPLACE VIEW vw_bi_vistoria_multivalorado AS
SELECT vistoria_id, ocorrencia_id, protocolo, data_vistoria, bairro, bairro_chave,
       campo, valor
FROM (
    SELECT v."Id" AS vistoria_id, v."OcorrenciaId" AS ocorrencia_id,
           o."Protocolo" AS protocolo, v."DataVistoria" AS data_vistoria,
           fn_bi_valor(l."Bairro") AS bairro,
           fn_bi_normaliza(fn_bi_valor(l."Bairro")) AS bairro_chave,
           x.campo, fn_bi_valor(x.valor) AS valor
    FROM vistorias v
    JOIN ocorrencias o ON o."Id" = v."OcorrenciaId"
    LEFT JOIN localizacoes l ON l."OcorrenciaId" = o."Id"
    CROSS JOIN LATERAL (
        SELECT 'Tipificação'            AS campo, unnest(v."TipificacaoOcorrencia")  AS valor
        UNION ALL
        SELECT 'Motivação',                        unnest(v."Motivacao")
        UNION ALL
        SELECT 'Área afetada',                     unnest(v."AreasAfetadas")
        UNION ALL
        SELECT 'Orientação',                       unnest(v."Orientacoes")
        UNION ALL
        SELECT 'Encaminhamento de campo',          unnest(v."EncaminhamentosDeCampo")
    ) x
    WHERE o."DeletedAt" IS NULL
) t
WHERE valor IS NOT NULL;

COMMENT ON VIEW vw_bi_vistoria_multivalorado IS
    'Campos de múltipla seleção da vistoria em formato longo (campo, valor). '
    'Filtre por "campo" para obter tipificação, motivação, áreas afetadas, '
    'orientações ou encaminhamentos de campo.';


-- ════════════════════════════════════════════════════════════════════════════
--  4. TIPO DE RISCO — ranking das causas
-- ════════════════════════════════════════════════════════════════════════════
--  Combina as duas fontes de tipificação: o campo texto TipoRisco e o
--  multivalorado TipificacaoOcorrencia. Ambos hoje vazios na carga histórica.
CREATE OR REPLACE VIEW vw_bi_tipo_risco AS
WITH fontes AS (
    SELECT ocorrencia_id, protocolo, data_vistoria, bairro_chave, bairro,
           grau_risco_encontrado, risco_alto, tipo_risco AS valor
    FROM vw_bi_vistorias
    WHERE tipo_risco IS NOT NULL
    UNION ALL
    SELECT m.ocorrencia_id, m.protocolo, m.data_vistoria, m.bairro_chave, m.bairro,
           v.grau_risco_encontrado, v.risco_alto, m.valor
    FROM vw_bi_vistoria_multivalorado m
    JOIN vw_bi_vistorias v ON v.vistoria_id = m.vistoria_id
    WHERE m.campo = 'Tipificação'
)
SELECT
    -- Agrupa pela chave normalizada e exibe a grafia mais frequente. Sem isso,
    -- "AVALIAÇÃO DE RISCO" e "AVALIAÇAO DE RISCO" viram dois tipos distintos.
    mode() WITHIN GROUP (ORDER BY valor)    AS tipo_risco,
    fn_bi_normaliza(valor)                  AS tipo_risco_chave,
    count(*)                                AS qtd_vistorias,
    count(*) FILTER (WHERE risco_alto)      AS qtd_risco_alto,
    round(100.0 * count(*) FILTER (WHERE risco_alto) / nullif(count(*), 0), 1) AS pct_risco_alto,
    count(DISTINCT bairro_chave)            AS qtd_bairros,
    min(data_vistoria)                      AS primeira_ocorrencia,
    max(data_vistoria)                      AS ultima_ocorrencia
FROM fontes
GROUP BY fn_bi_normaliza(valor);

COMMENT ON VIEW vw_bi_tipo_risco IS
    'Ranking dos tipos de risco constatados em campo. Vazia enquanto TipoRisco e '
    'TipificacaoOcorrencia não forem preenchidos nas vistorias.';


-- ════════════════════════════════════════════════════════════════════════════
--  4b. TIPIFICAÇÃO POR OCORRÊNCIA — uma linha por (ocorrência, tipificação)
-- ════════════════════════════════════════════════════════════════════════════
--  Formato longo, já carregando o ANO de abertura da ocorrência. Isso permite
--  fatiar o ranking de tipos por ano no Power BI sem precisar de relacionamento
--  entre tabelas. Uma ocorrência com duas tipificações aparece em duas linhas —
--  some por ocorrência distinta, não por linha, quando o total importar.
CREATE OR REPLACE VIEW vw_bi_tipificacao_ocorrencia AS
SELECT
    o.ocorrencia_id,
    o.protocolo,
    o.ano,
    o.mes,
    o.ano_mes,
    o.periodo_chuvoso,
    o.bairro,
    o.bairro_chave,
    o.grau_risco_encontrado,
    o.risco_alto,
    o.houve_interdicao,
    o.em_aberto,
    m.valor                     AS tipificacao,
    fn_bi_normaliza(m.valor)    AS tipificacao_chave
FROM vw_bi_ocorrencias o
JOIN vw_bi_vistoria_multivalorado m ON m.ocorrencia_id = o.ocorrencia_id
WHERE m.campo = 'Tipificação';

COMMENT ON VIEW vw_bi_tipificacao_ocorrencia IS
    'Tipificações da vistoria em formato longo, com o ano da ocorrência — base '
    'do ranking de tipos de risco fatiável por ano.';


-- ════════════════════════════════════════════════════════════════════════════
--  5. POPULAÇÃO EXPOSTA — por bairro e grau de risco
-- ════════════════════════════════════════════════════════════════════════════
--  O indicador mais forte para justificar recurso e priorizar remoção.
--  Hoje zerado: a planilha histórica não registrava moradores.
CREATE OR REPLACE VIEW vw_bi_populacao_exposta AS
SELECT
    coalesce(bairro, 'Não informado')            AS bairro,
    bairro_chave,
    coalesce(grau_risco_encontrado, 'Sem classificação') AS grau_risco,
    count(*)                                     AS qtd_vistorias,
    count(*) FILTER (WHERE risco_alto)           AS qtd_risco_alto,
    count(*) FILTER (WHERE houve_interdicao)     AS qtd_interdicoes,
    sum(total_moradores)                         AS total_moradores,
    sum(adultos)                                 AS adultos,
    sum(criancas)                                AS criancas,
    sum(idosos)                                  AS idosos,
    sum(pessoas_com_deficiencia)                 AS pessoas_com_deficiencia,
    sum(nullif(pessoas_vulneraveis, 0))          AS pessoas_vulneraveis,
    sum(numero_moradias)                         AS moradias,
    round(avg(total_moradores), 2)               AS media_moradores_por_imovel,
    count(*) FILTER (WHERE total_moradores IS NOT NULL) AS vistorias_com_moradores
FROM vw_bi_vistorias
GROUP BY 1, 2, 3;

COMMENT ON VIEW vw_bi_populacao_exposta IS
    'População exposta por bairro e grau de risco: moradores, crianças, idosos e '
    'pessoas com deficiência. Somas ficam nulas enquanto a composição familiar '
    'não for preenchida nas vistorias.';


-- ════════════════════════════════════════════════════════════════════════════
--  6. SÉRIE MENSAL
-- ════════════════════════════════════════════════════════════════════════════
CREATE OR REPLACE VIEW vw_bi_serie_mensal AS
SELECT
    ano_mes,
    ano_mes_rotulo,
    ano,
    mes,
    periodo_chuvoso,
    count(*)                                          AS ocorrencias_abertas,
    count(*) FILTER (WHERE tem_vistoria)              AS com_vistoria,
    count(*) FILTER (WHERE risco_alto)                AS risco_alto,
    count(*) FILTER (WHERE houve_interdicao)          AS interdicoes,
    count(*) FILTER (WHERE em_aberto)                 AS ainda_em_aberto,
    count(*) FILTER (WHERE situacao_relatorio = 'Pendente') AS relatorios_pendentes,
    round(avg(dias_ate_vistoria), 1)                  AS media_dias_ate_vistoria,
    percentile_cont(0.5) WITHIN GROUP (ORDER BY dias_ate_vistoria) AS mediana_dias_ate_vistoria
FROM vw_bi_ocorrencias
GROUP BY 1, 2, 3, 4, 5;

COMMENT ON VIEW vw_bi_serie_mensal IS 'Volume e desempenho por mês de abertura.';


-- ════════════════════════════════════════════════════════════════════════════
--  7. SAZONALIDADE — por mês do ano, somando todos os anos
-- ════════════════════════════════════════════════════════════════════════════
CREATE OR REPLACE VIEW vw_bi_sazonalidade AS
SELECT
    mes,
    to_char(to_date(mes::text, 'MM'), 'TMMonth')      AS mes_nome,
    periodo_chuvoso,
    count(*)                                          AS ocorrencias,
    count(*) FILTER (WHERE risco_alto)                AS risco_alto,
    count(*) FILTER (WHERE houve_interdicao)          AS interdicoes,
    round(100.0 * count(*) / nullif(sum(count(*)) OVER (), 0), 1) AS pct_do_total,
    count(DISTINCT ano)                               AS anos_observados,
    round(count(*)::numeric / nullif(count(DISTINCT ano), 0), 1)  AS media_por_ano
FROM vw_bi_ocorrencias
GROUP BY 1, 2, 3;

COMMENT ON VIEW vw_bi_sazonalidade IS
    'Distribuição por mês do ano. Base para dimensionar plantão no período chuvoso.';


-- ════════════════════════════════════════════════════════════════════════════
--  8. TEMPO DE RESPOSTA — distribuição por faixa
-- ════════════════════════════════════════════════════════════════════════════
CREATE OR REPLACE VIEW vw_bi_tempo_resposta AS
SELECT
    faixa_tempo_vistoria                              AS faixa,
    CASE faixa_tempo_vistoria
        WHEN '0-3 dias'   THEN 1
        WHEN '4-7 dias'   THEN 2
        WHEN '8-15 dias'  THEN 3
        WHEN '16-30 dias' THEN 4
        WHEN '31+ dias'   THEN 5
    END                                               AS faixa_ordem,
    count(*)                                          AS qtd_vistorias,
    round(100.0 * count(*) / nullif(sum(count(*)) OVER (), 0), 1) AS pct,
    min(dias_ate_vistoria)                            AS dias_min,
    max(dias_ate_vistoria)                            AS dias_max,
    count(*) FILTER (WHERE risco_alto)                AS qtd_risco_alto
FROM vw_bi_ocorrencias
WHERE dias_ate_vistoria IS NOT NULL
GROUP BY 1, 2;

COMMENT ON VIEW vw_bi_tempo_resposta IS
    'Distribuição do tempo entre abertura e vistoria. Exclui registros com data '
    'inconsistente (fora de 0 a 365 dias).';


-- ════════════════════════════════════════════════════════════════════════════
--  9. INDICADORES CONSOLIDADOS — uma linha, para os cartões do topo
-- ════════════════════════════════════════════════════════════════════════════
CREATE OR REPLACE VIEW vw_bi_indicadores AS
SELECT
    count(*)                                                  AS total_ocorrencias,
    count(*) FILTER (WHERE em_aberto)                         AS em_aberto,
    count(*) FILTER (WHERE NOT em_aberto)                     AS arquivadas,
    count(*) FILTER (WHERE status = 'ABERTA')                 AS aguardando_triagem,
    count(*) FILTER (WHERE status = 'VISTORIA_SOLICITADA')    AS aguardando_vistoria,
    count(*) FILTER (WHERE status = 'VISTORIA_REALIZADA')     AS aguardando_encerramento,
    count(*) FILTER (WHERE tem_vistoria)                      AS vistorias_concluidas,
    count(*) FILTER (WHERE situacao_relatorio = 'Concluído')  AS relatorios_concluidos,
    count(*) FILTER (WHERE situacao_relatorio = 'Pendente')   AS relatorios_pendentes,
    count(*) FILTER (WHERE risco_alto)                        AS risco_alto,
    count(*) FILTER (WHERE houve_interdicao)                  AS interdicoes,
    count(*) FILTER (WHERE emergencia)                        AS emergencias,
    sum(total_moradores)                                      AS moradores_atendidos,
    sum(nullif(pessoas_vulneraveis, 0))                       AS pessoas_vulneraveis,

    round(avg(dias_ate_vistoria), 1)                          AS media_dias_ate_vistoria,
    percentile_cont(0.5) WITHIN GROUP (ORDER BY dias_ate_vistoria) AS mediana_dias_ate_vistoria,
    percentile_cont(0.9) WITHIN GROUP (ORDER BY dias_ate_vistoria) AS p90_dias_ate_vistoria,
    round(100.0 * count(*) FILTER (WHERE dias_ate_vistoria <= 7)
                / nullif(count(*) FILTER (WHERE dias_ate_vistoria IS NOT NULL), 0), 1)
                                                              AS pct_atendidas_em_7_dias,
    round(avg(dias_vistoria_ate_relatorio), 1)                AS media_dias_ate_relatorio,
    max(dias_em_aberto)                                       AS maior_espera_em_aberto,

    -- Produtividade na definição da planilha atual: concluídas sobre o total
    round(100.0 * count(*) FILTER (WHERE tem_vistoria) / nullif(count(*), 0), 1)
                                                              AS produtividade_vistorias,
    round(100.0 * count(*) FILTER (WHERE situacao_relatorio = 'Concluído')
                / nullif(count(*), 0), 1)                     AS produtividade_relatorios
FROM vw_bi_ocorrencias;

COMMENT ON VIEW vw_bi_indicadores IS
    'Linha única com os indicadores de topo do painel. As duas medidas de '
    'produtividade seguem a definição concluídas/total.';


-- ════════════════════════════════════════════════════════════════════════════
-- 10. TERRITÓRIO — por bairro
-- ════════════════════════════════════════════════════════════════════════════
CREATE OR REPLACE VIEW vw_bi_bairros AS
SELECT
    -- Grafia mais frequente do bairro, para exibir sem o ruído de acentuação
    mode() WITHIN GROUP (ORDER BY bairro)             AS bairro,
    bairro_chave,
    count(*)                                          AS ocorrencias,
    count(*) FILTER (WHERE risco_alto)                AS risco_alto,
    round(100.0 * count(*) FILTER (WHERE risco_alto) / nullif(count(*), 0), 1) AS pct_risco_alto,
    count(*) FILTER (WHERE houve_interdicao)          AS interdicoes,
    count(*) FILTER (WHERE em_aberto)                 AS em_aberto,
    sum(total_moradores)                              AS total_moradores,
    sum(nullif(pessoas_vulneraveis, 0))               AS pessoas_vulneraveis,
    round(avg(dias_ate_vistoria), 1)                  AS media_dias_ate_vistoria,
    count(*) FILTER (WHERE periodo_chuvoso)           AS ocorrencias_periodo_chuvoso,
    min(data_abertura)                                AS primeira_ocorrencia,
    max(data_abertura)                                AS ultima_ocorrencia
FROM vw_bi_ocorrencias
WHERE bairro_chave IS NOT NULL
GROUP BY bairro_chave;

COMMENT ON VIEW vw_bi_bairros IS
    'Consolidado por bairro, com a grafia unificada por fn_bi_normaliza.';


-- ════════════════════════════════════════════════════════════════════════════
-- 11. FILA OPERACIONAL — o que está em aberto e há quanto tempo
-- ════════════════════════════════════════════════════════════════════════════
CREATE OR REPLACE VIEW vw_bi_backlog AS
SELECT
    ocorrencia_id,
    protocolo,
    data_abertura,
    status,
    status_ordem,
    bairro,
    grau_risco_encontrado,
    risco_alto,
    situacao_relatorio,
    data_vistoria,
    dias_em_aberto,
    faixa_aging,
    -- Prioriza risco alto primeiro, depois tempo de espera
    (CASE WHEN risco_alto THEN 1000 ELSE 0 END + coalesce(dias_em_aberto, 0)) AS score_prioridade
FROM vw_bi_ocorrencias
WHERE em_aberto;

COMMENT ON VIEW vw_bi_backlog IS
    'Ocorrências não encerradas, com envelhecimento e um score de priorização '
    'que coloca risco alto à frente do tempo de espera.';


-- ════════════════════════════════════════════════════════════════════════════
-- 12. CARGA POR VISTORIADOR
-- ════════════════════════════════════════════════════════════════════════════
--  Conta os quatro membros da equipe, não só o responsável principal — é a
--  única forma de medir a carga real de campo.
CREATE OR REPLACE VIEW vw_bi_carga_vistoriador AS
WITH participacoes AS (
    SELECT v.vistoria_id, v.ocorrencia_id, v.data_vistoria, v.ano_vistoria,
           v.ano_mes_vistoria, v.risco_alto, v.duracao_minutos, v.bairro_chave,
           x.usuario_id, x.papel
    FROM vw_bi_vistorias v
    CROSS JOIN LATERAL (
        SELECT v.vistoriador1_id AS usuario_id, 'Responsável' AS papel
        UNION ALL SELECT v.vistoriador2_id, 'Apoio'
        UNION ALL SELECT v.vistoriador3_id, 'Apoio'
        UNION ALL SELECT v.vistoriador4_id, 'Apoio'
    ) x
    WHERE x.usuario_id IS NOT NULL
)
SELECT
    p.usuario_id                                   AS vistoriador_id,
    u."Nome"                                       AS vistoriador,
    u."Ativo"                                      AS ativo,
    -- Conta técnica criada pela importação; não é uma pessoa da equipe
    (u."Email" = 'importacao@sig.defesacivil.local') AS conta_de_sistema,
    count(*)                                       AS vistorias,
    count(*) FILTER (WHERE p.papel = 'Responsável') AS como_responsavel,
    count(*) FILTER (WHERE p.papel = 'Apoio')       AS como_apoio,
    count(*) FILTER (WHERE p.risco_alto)            AS vistorias_risco_alto,
    count(DISTINCT p.bairro_chave)                  AS bairros_atendidos,
    round(avg(p.duracao_minutos), 1)                AS media_minutos_por_vistoria,
    min(p.data_vistoria)                            AS primeira_vistoria,
    max(p.data_vistoria)                            AS ultima_vistoria
FROM participacoes p
JOIN usuarios u ON u."Id" = p.usuario_id
GROUP BY 1, 2, 3, 4;

COMMENT ON VIEW vw_bi_carga_vistoriador IS
    'Carga de campo por vistoriador, contando os quatro membros da equipe. '
    'A coluna conta_de_sistema marca o usuário técnico da importação.';


-- ════════════════════════════════════════════════════════════════════════════
-- 13. ENCAMINHAMENTOS AOS ÓRGÃOS
-- ════════════════════════════════════════════════════════════════════════════
--  Mede o gargalo externo: para quem a Defesa Civil encaminha e quanto tempo
--  leva até o encerramento. Vazia enquanto não houver encaminhamentos finais.
CREATE OR REPLACE VIEW vw_bi_encaminhamentos AS
SELECT
    fn_bi_encaminhamento(cod)                      AS orgao,
    cod                                            AS codigo,
    count(*)                                       AS qtd,
    count(*) FILTER (WHERE o.risco_alto)           AS qtd_risco_alto,
    count(*) FILTER (WHERE ef."RetornoEncaminhamentos" IS NOT NULL
                       AND btrim(ef."RetornoEncaminhamentos") <> '') AS com_retorno,
    round(avg(o.dias_ate_encerramento), 1)         AS media_dias_ate_encerramento,
    min(ef."RegistradoEm")                         AS primeiro_encaminhamento,
    max(ef."RegistradoEm")                         AS ultimo_encaminhamento
FROM encaminhamentos_finais ef
JOIN vw_bi_ocorrencias o ON o.ocorrencia_id = ef."OcorrenciaId"
CROSS JOIN LATERAL unnest(ef."Encaminhamentos") AS cod
GROUP BY 1, 2;

COMMENT ON VIEW vw_bi_encaminhamentos IS
    'Encaminhamentos formais por órgão (CEMIG, COPASA, Sec. Obras…), com taxa de '
    'retorno. Vazia enquanto a etapa 6 não for usada.';


-- ════════════════════════════════════════════════════════════════════════════
-- 14. NOTIFICADOS — entrega do relatório
-- ════════════════════════════════════════════════════════════════════════════
CREATE OR REPLACE VIEW vw_bi_notificados AS
SELECT
    n."FormaRecebimento"                           AS forma_recebimento,
    date_trunc('month', n."DataNotificacao")::date AS ano_mes,
    count(*)                                       AS qtd,
    count(DISTINCT n."OcorrenciaId")               AS ocorrencias,
    round(avg(n."DataNotificacao" - o.data_vistoria), 1) AS media_dias_vistoria_ate_notificacao
FROM notificados n
JOIN vw_bi_ocorrencias o ON o.ocorrencia_id = n."OcorrenciaId"
GROUP BY 1, 2;

COMMENT ON VIEW vw_bi_notificados IS
    'Entrega do relatório por forma de recebimento (e-mail ou presencial). '
    'Vazia enquanto não houver notificados registrados.';


-- ════════════════════════════════════════════════════════════════════════════
-- 15. QUALIDADE DOS DADOS — o painel de confiabilidade, auto-atualizável
-- ════════════════════════════════════════════════════════════════════════════
--  Mede sozinha o preenchimento de cada campo. Conforme o sistema for usado, os
--  percentuais sobem e as linhas mudam de "Sem dado" para "Confiável" — serve
--  como acompanhamento da migração do papel para o digital.
CREATE OR REPLACE VIEW vw_bi_qualidade_dados AS
WITH base AS (SELECT count(*)::numeric AS n FROM vw_bi_vistorias),
     baseo AS (SELECT count(*)::numeric AS n FROM vw_bi_ocorrencias),
     medidas AS (
    SELECT 'Vistoria'   AS escopo, 'Tipo de risco'            AS campo,
           count(*) FILTER (WHERE tipo_risco IS NOT NULL)::numeric AS preenchidos,
           (SELECT n FROM base) AS total FROM vw_bi_vistorias
    UNION ALL SELECT 'Vistoria', 'Grau de risco constatado',
           count(*) FILTER (WHERE grau_risco_encontrado IS NOT NULL), (SELECT n FROM base) FROM vw_bi_vistorias
    UNION ALL SELECT 'Vistoria', 'Interdição',
           count(*) FILTER (WHERE interdicao IS NOT NULL), (SELECT n FROM base) FROM vw_bi_vistorias
    UNION ALL SELECT 'Vistoria', 'Remoção',
           count(*) FILTER (WHERE remocao IS NOT NULL), (SELECT n FROM base) FROM vw_bi_vistorias
    UNION ALL SELECT 'Vistoria', 'Regime de ocupação',
           count(*) FILTER (WHERE regime_ocupacao IS NOT NULL), (SELECT n FROM base) FROM vw_bi_vistorias
    UNION ALL SELECT 'Vistoria', 'Moradores por imóvel',
           count(*) FILTER (WHERE total_moradores IS NOT NULL), (SELECT n FROM base) FROM vw_bi_vistorias
    UNION ALL SELECT 'Vistoria', 'Composição familiar',
           count(*) FILTER (WHERE adultos IS NOT NULL OR criancas IS NOT NULL
                              OR idosos IS NOT NULL OR pessoas_com_deficiencia IS NOT NULL),
           (SELECT n FROM base) FROM vw_bi_vistorias
    UNION ALL SELECT 'Vistoria', 'Caracterização do local',
           count(*) FILTER (WHERE caracterizacao_local IS NOT NULL), (SELECT n FROM base) FROM vw_bi_vistorias
    UNION ALL SELECT 'Vistoria', 'Data consistente',
           count(*) FILTER (WHERE dias_ate_vistoria IS NOT NULL), (SELECT n FROM base) FROM vw_bi_vistorias
    UNION ALL SELECT 'Ocorrência', 'Bairro',
           count(*) FILTER (WHERE bairro IS NOT NULL), (SELECT n FROM baseo) FROM vw_bi_ocorrencias
    UNION ALL SELECT 'Ocorrência', 'Coordenada geográfica',
           count(*) FILTER (WHERE tem_coordenada), (SELECT n FROM baseo) FROM vw_bi_ocorrencias
    UNION ALL SELECT 'Ocorrência', 'Avaliação de risco (etapa 2)',
           count(*) FILTER (WHERE tem_avaliacao_risco), (SELECT n FROM baseo) FROM vw_bi_ocorrencias
    UNION ALL SELECT 'Ocorrência', 'Encaminhamento final (etapa 6)',
           count(*) FILTER (WHERE tem_encaminhamento_final), (SELECT n FROM baseo) FROM vw_bi_ocorrencias
    UNION ALL SELECT 'Ocorrência', 'Notificados',
           count(*) FILTER (WHERE qtd_notificados > 0), (SELECT n FROM baseo) FROM vw_bi_ocorrencias
)
SELECT
    escopo,
    campo,
    preenchidos::bigint                                   AS registros_preenchidos,
    total::bigint                                         AS registros_totais,
    round(100.0 * preenchidos / nullif(total, 0), 1)      AS pct_preenchido,
    CASE
        WHEN total = 0 THEN 'Sem base'
        WHEN preenchidos = 0 THEN 'Sem dado'
        WHEN 100.0 * preenchidos / total < 70 THEN 'Parcial'
        ELSE 'Confiável'
    END                                                   AS situacao
FROM medidas;

COMMENT ON VIEW vw_bi_qualidade_dados IS
    'Preenchimento de cada campo que alimenta o BI. Recalculada a cada consulta: '
    'acompanha a evolução da base conforme o sistema entra em uso.';
