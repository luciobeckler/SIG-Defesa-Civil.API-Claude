namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Chaves dos campos de seleção da vistoria que aceitam opções personalizadas.
    /// Devem ficar em sincronia com as chaves usadas no frontend.
    /// </summary>
    public static class CamposVistoria
    {
        public const string Tipificacao        = "TIPIFICACAO";
        public const string GrauRisco          = "GRAU_RISCO";
        public const string Edificacao         = "EDIFICACAO";
        public const string Estrutura          = "ESTRUTURA";
        public const string TipoRisco          = "TIPO_RISCO";
        public const string RegimeOcupacao     = "REGIME_OCUPACAO";
        public const string AreaAfetada        = "AREA_AFETADA";
        public const string Interdicao         = "INTERDICAO";
        public const string Remocao            = "REMOCAO";
        public const string Motivacao          = "MOTIVACAO";
        public const string Orientacao         = "ORIENTACAO";
        public const string Encaminhamento     = "ENCAMINHAMENTO";
        public const string CaracterizacaoLocal = "CARACTERIZACAO_LOCAL";

        public static readonly IReadOnlySet<string> Todos = new HashSet<string>
        {
            Tipificacao, GrauRisco, Edificacao, Estrutura, TipoRisco, RegimeOcupacao,
            AreaAfetada, Interdicao, Remocao, Motivacao, Orientacao, Encaminhamento,
            CaracterizacaoLocal,
        };
    }
}
