using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIG_Defesa_Civil.API.Data.Models.Tabelas
{
    /// <summary>
    /// Endereço e dados de georreferenciamento da ocorrência.
    /// Entidade dependente de Ocorrencia (1:1) — não existe sem uma ocorrência.
    /// Separada para manter a tabela principal enxuta e habilitar queries espaciais futuras.
    /// </summary>
    [Table("localizacoes")]
    public class Localizacao
    {
        public int Id { get; set; }

        // FK para a ocorrência dona deste endereço
        public int OcorrenciaId { get; set; }
        public Ocorrencia Ocorrencia { get; set; } = null!;

        // ── Endereço estruturado ─────────────────────────────────────────────────
        public string Endereco { get; set; } = null!;
        public string Bairro { get; set; } = null!;
        public string? Numero { get; set; }
        public string? Cep { get; set; }
        public string? Complemento { get; set; }
        public string Cidade { get; set; } = null!;
        public string Uf { get; set; } = null!;

        // ── Georeferenciamento ───────────────────────────────────────────────────
        /// <summary>Coordenada GPS em texto livre (ex: "-19.8822, -43.8922").</summary>
        public string? Coordenada { get; set; }
        public string? Referencia { get; set; }

        // ── Dados cadastrais do imóvel ───────────────────────────────────────────
        public string? NumeroIptu { get; set; }
    }
}
