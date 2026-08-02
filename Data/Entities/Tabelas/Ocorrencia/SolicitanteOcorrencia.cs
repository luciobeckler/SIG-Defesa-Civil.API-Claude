namespace SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia
{
    /// <summary>
    /// Dados do cidadão que abriu a ocorrência. É um tipo <b>owned</b> — as colunas
    /// moram na própria tabela <c>ocorrencias</c>, não há registro em <c>usuarios</c>.
    ///
    /// Cidadãos não têm conta no sistema (a abertura é um endpoint público), então
    /// tratá-los como usuário só poluía a tabela de colaboradores. Além disso, guardar
    /// os dados aqui os congela no momento da abertura: uma nova ocorrência do mesmo
    /// CPF não reescreve mais o nome/contato das ocorrências antigas.
    /// </summary>
    public class SolicitanteOcorrencia
    {
        public string Nome { get; set; } = null!;

        /// <summary>Somente dígitos. Nulo apenas em registros históricos importados da planilha.</summary>
        public string? Cpf { get; set; }

        public string? Rg { get; set; }
        /// <summary>Órgão emissor do RG (ex.: SSP/MG).</summary>
        public string? OrgaoEmissor { get; set; }

        /// <summary>Obrigatório na abertura pública; nulo apenas em registros históricos.</summary>
        public string? Email { get; set; }

        public string? Telefone { get; set; }
        /// <summary>Celular — campo separado de Telefone (fixo).</summary>
        public string? Celular { get; set; }
    }
}
