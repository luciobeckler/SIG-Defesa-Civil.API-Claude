namespace SIG_Defesa_Civil.API.Enums
{
    // ─── Usuários ────────────────────────────────────────────────────────────────
    public enum TipoUsuario { CIDADAO, ATENDENTE, VISTORIADOR, ADMIN }

    // ─── Arquivos ────────────────────────────────────────────────────────────────
    public enum TipoArquivo { FOTO_CIDADAO, COMPROVANTE_RESIDENCIA, FICHA_VISTORIA, FOTO_CAMPO, RELATORIO_FINAL }

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
        DESLIZAMENTO,
        RISCO_ESTRUTURAL,
        ENCHENTE,
        EROSAO,
        DESABAMENTO,
        ALAGAMENTO
    }

    public enum GrauRisco
    {
        SEM_RISCO,
        BAIXO,
        MEDIO,
        ALTO,
        MUITO_ALTO
    }

    // ─── Etapa 4 — Vistoria Presencial ───────────────────────────────────────────
    public enum TipoEdificacao
    {
        RESIDENCIAL,
        COMERCIAL,
        MISTO,
        PUBLICO
    }

    public enum TipoEstrutura
    {
        ALVENARIA,
        MADEIRA,
        MISTA,
        CONCRETO
    }

    public enum TipoRiscoVistoria
    {
        GEOLOGICO,
        HIDROLOGICO,
        ESTRUTURAL,
        TECNOLOGICO
    }

    public enum RegimeOcupacaoImovel
    {
        PROPRIO,
        ALUGADO,
        CEDIDO,
        IRREGULAR
    }

    public enum AreaAfetada
    {
        ESTRUTURA,
        TELHADO,
        FUNDACAO,
        MURO,
        PISO,
        AREA_EXTERNA
    }

    public enum TipoInterdicao
    {
        NAO_INTERDITADO,
        INTERDITADO_PARCIAL,
        INTERDITADO_TOTAL
    }

    public enum TipoRemocao
    {
        NAO_REMOVIDA,
        REMOVIDA_TEMPORARIA,
        REMOVIDA_DEFINITIVA
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
