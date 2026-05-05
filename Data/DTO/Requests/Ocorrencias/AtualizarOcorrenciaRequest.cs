using SIG_Defesa_Civil.API.Data.DTO.Requests.Usuarios;
using System.ComponentModel.DataAnnotations;

namespace SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias
{
    /// <summary>
    /// DTO para edição dos dados da Etapa 1 após a abertura.
    /// Todos os campos são opcionais — apenas o que for enviado será atualizado.
    /// </summary>
    public class AtualizarOcorrenciaRequest
    {
        /// <summary>Dados pessoais atualizados do solicitante.</summary>
        public CidadaoDto? Cidadao { get; set; }

        /// <summary>Endereço atualizado.</summary>
        public LocalOcorrenciaDto? Local { get; set; }

        [MinLength(10, ErrorMessage = "Descrição deve ter no mínimo 10 caracteres.")]
        public string? DescricaoProblema { get; set; }
    }
}
