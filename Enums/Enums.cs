namespace SIG_Defesa_Civil.API.Enums
{
    // ─── Usuários ────────────────────────────────────────────────────────────────
    public enum TipoUsuario { CIDADAO, ATENDENTE, VISTORIADOR, ADMIN }

    // ─── Arquivos ────────────────────────────────────────────────────────────────
    public enum TipoArquivo { FOTO_CIDADAO, COMPROVANTE_RESIDENCIA, FICHA_VISTORIA, FOTO_CAMPO, RELATORIO_FINAL, RELATORIO_ASSINADO, ASSINATURA_MUNICIPIO }

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

    /// <summary>Turno da visita agendada.</summary>
    public enum TurnoVistoria { MANHA, TARDE }

    // ─── Etapa 4 — Vistoria Presencial ───────────────────────────────────────────
    // Os campos de seleção da vistoria (edificação, estrutura, tipo de risco, grau,
    // áreas afetadas, interdição, remoção, motivação, orientações, caracterização)
    // deixaram de ser enums fixos: agora são texto, com as opções fixas no frontend
    // (enum-options.ts) e opções personalizadas no catálogo (OpcaoCampoVistoria).

    /// <summary>
    /// Encaminhamentos institucionais — usado pelo EncaminhamentoFinal (Etapa 6).
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

    // ─── Notificados (propriedade da ocorrência) ─────────────────────────────────
    /// <summary>
    /// Como o notificado recebeu o relatório. PRESENCIAL exige coleta de assinatura.
    /// A entrega institucional do relatório é sempre por e-mail.
    /// </summary>
    public enum FormaRecebimentoRelatorio
    {
        EMAIL,
        PRESENCIAL
    }

    // ─── Legado (mantido para compatibilidade com código existente) ───────────────
    /// <obsolete>Substituído por GrauRisco a partir da v2. Remover após migração completa.</obsolete>
    public enum GravidadeOcorrencia { BAIXA, MEDIA, ALTA, MUITO_ALTA }
}
