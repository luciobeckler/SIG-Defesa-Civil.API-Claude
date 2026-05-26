namespace SIG_Defesa_Civil.API.Enums
{
    // ─── Usuários ────────────────────────────────────────────────────────────────
    public enum TipoUsuario { CIDADAO, ATENDENTE, VISTORIADOR, ADMIN }

    // ─── Arquivos ────────────────────────────────────────────────────────────────
    public enum TipoArquivo { FOTO_CIDADAO, COMPROVANTE_RESIDENCIA, FICHA_VISTORIA, FOTO_CAMPO, RELATORIO_FINAL, ASSINATURA_MUNICIPIO }

    // ─── LGPD ────────────────────────────────────────────────────────────────────
    public enum AcaoLgpd { VISUALIZOU, BAIXOU, EDITOU, EXCLUIU, CRIOU }

    // ─── Infraestrutura ──────────────────────────────────────────────────────────
    public enum ConnectionStrings { PRODCONNECTION, DEVCONNECTION }
    public enum ErrosRequisicoes
    {
        UPLOAD_FAILED, ERRO_PROCESSAMENTO, ARQUIVO_MUITO_GRANDE, ARQUIVOS_AUSENTES,
        VALIDACAO_FALHOU, DADOS_INVALIDOS, JSON_INVALIDO, DADOS_AUSENTES, ERRO_INTERNO,
        ACESSO_NEGADO
    }
    public enum StorageErrorType
    {
        Generico, PermissaoNegada, DiscoLotado, CaminhoInvalido, ArquivoNaoEncontrado, ErroLeituraEscrita
    }

    // ─── Ciclo de vida da ocorrência (máquina de estados) ────────────────────────
    /// <summary>
    /// Cada valor corresponde a uma etapa do fluxo operacional.
    /// ABERTA = Etapa 1 concluída | EM_AVALIACAO = Etapa 2 | VISTORIA_SOLICITADA = Etapa 3
    /// VISTORIA_REALIZADA = Etapa 4 | NOTIFICADA = Etapa 5 | ENCERRADA = Etapa 6 | CANCELADA = soft-delete
    /// </summary>
    public enum StatusOcorrencia
    {
        ABERTA,
        EM_AVALIACAO,
        VISTORIA_SOLICITADA,
        VISTORIA_REALIZADA,
        NOTIFICADA,
        ENCERRADA,
        CANCELADA
    }

    // ─── Etapa 2 — Avaliação de Risco ────────────────────────────────────────────
    public enum TipificacaoOcorrencia
    {
        ABATIMENTO_DE_FOSSA,
        ALAGAMENTO,
        ARVORE_COM_RISCO_DE_QUEDA,
        CICATRIZ_DE_ESCORREGAMENTO,
        DEGRAU_DE_ABATIMENTO,
        EROSAO,
        ESCORREGAMENTO,
        INCENDIO,
        INUNDACAO_DE_CORREGO_RIO,
        QUEDA_DE_ARVORES,
        REDE_PUBLICA_DE_DRENAGEM_PLUVIAL_ROMPIDA,
        ROLAMENTO_TOMBAMENTO_DE_BLOCOS,
        SOLAPAMENTO,
        TRINCAS
    }

    public enum GrauRisco
    {
        BAIXO,
        MEDIO,
        ALTO,
        MUITO_ALTO
    }

    // ─── Etapa 3 — Agendamento de Vistoria ──────────────────────────────────────
    /// <summary>
    /// Estado do ciclo de vida de um agendamento.
    /// ATIVO = aguardando comparecimento | CONCLUIDO = vistoria realizada | CANCELADO = descartado
    /// </summary>
    public enum StatusAgendamento
    {
        ATIVO,
        CONCLUIDO,
        CANCELADO
    }

    // ─── Etapa 4 — Vistoria Presencial ───────────────────────────────────────────

    /// <summary>Caracterização geomorfológica do local vistoriado.</summary>
    public enum CaracterizacaoLocal
    {
        DE_CORTE,
        ENCOSTA_MORRO,
        MARGEM_CORREGO_RIO,
        RURAL,
        URBANA
    }

    public enum TipoEdificacao
    {
        BARRACAO,
        CASA,
        COMERCIO,
        GALPAO,
        PREDIO
    }

    public enum TipoEstrutura
    {
        ALVENARIA,
        CONCRETO_ARMADO,
        MADEIRA,
        OUTROS_MATERIAIS,
        PRE_FABRICADO
    }

    public enum TipoRiscoVistoria
    {
        BIOLOGICO,
        CONSTRUTIVO,
        GEOLOGICO,
        HIDROLOGICO,
        TECNOLOGICO,
        OUTROS
    }

    public enum RegimeOcupacaoImovel
    {
        PROPRIO,
        ALUGADO,
        CEDIDO,
        IRREGULAR,
        OUTROS
    }

    /// <summary>Áreas do imóvel/entorno afetadas — multi-select.</summary>
    public enum AreaAfetada
    {
        COMERCIO,
        GALPAO,
        MURO,
        OUTROS,
        PONTE,
        PREDIO_PUBLICO,
        RESIDENCIA,
        VIA_PUBLICA
    }

    public enum TipoInterdicao
    {
        NAO_NECESSARIA,
        PARCIAL,
        TOTAL
    }

    public enum TipoRemocao
    {
        NAO_NECESSARIA,
        TEMPORARIA,
        DEFINITIVA
    }

    /// <summary>Causas/motivações identificadas na vistoria — multi-select.</summary>
    public enum Motivacao
    {
        DESABAMENTO_PARCIAL,
        DESABAMENTO_TOTAL,
        DESPRENDIMENTO_DE_REBOCO,
        ENCOSTA,
        INFILTRACAO,
        LANCAMENTO_AGUA_PLUVIAL_ESGOTO,
        LANCAMENTO_LIXO_ENTULHO_ATERRO,
        MOVIMENTACAO_DE_SOLO,
        PRECARIO_INSALUBRE,
        RACHADURAS
    }

    /// <summary>Orientações dadas ao morador — multi-select.</summary>
    public enum Orientacao
    {
        CONTRATACAO_PROFISSIONAL_HABILITADO,
        DESOCUPACAO,
        NAO_EXPANDIR_EDIFICACAO,
        NAO_PERMANECER_EM_CASO_DE_CHUVA,
        NAO_PERMANECER_NO_LOCAL_ENQUANTO_HOUVER_RISCO,
        PROCURAR_ABRIGO,
        REALIZAR_CAPTACAO_AGUAS_PLUVIAIS,
        REMOCAO_DO_ENTULHO,
        SOLICITAR_NOVA_VISTORIA
    }

    /// <summary>
    /// Encaminhamentos institucionais — multi-select compartilhado entre
    /// Vistoria (campo) e EncaminhamentoFinal (Etapa 6).
    /// </summary>
    public enum Encaminhamento
    {
        AJUDA_HUMANITARIA,
        CEMIG,
        COPASA,
        DNIT,
        OUTROS,
        PROVIDENCIAS_PELO_MORADOR,
        SEC_DESENV_SOCIAL,
        SEC_MEIO_AMBIENTE,
        SEC_OBRAS
    }

    // ─── Etapa 6 — Encaminhamento Final ──────────────────────────────────────────
    public enum CanalEntregaRelatorio
    {
        EMAIL,
        WHATSAPP
    }

    // ─── Legado (mantido para compatibilidade com código existente) ───────────────
    /// <obsolete>Substituído por GrauRisco a partir da v2. Remover após migração completa.</obsolete>
    public enum GravidadeOcorrencia { BAIXA, MEDIA, ALTA, MUITO_ALTA }
}
