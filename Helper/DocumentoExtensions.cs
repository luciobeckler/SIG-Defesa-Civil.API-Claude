using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;

namespace SIG_Defesa_Civil.API.Helper
{
    /// <summary>
    /// Extension methods para facilitar geração de dicionários de dados para templates
    /// </summary>
    public static class DocumentoExtensions
    {
        /// <summary>
        /// Converte uma ocorrência em dicionário para substituição em templates
        /// </summary>
        public static Dictionary<string, string> ParaDicionarioTemplate(this Ocorrencia ocorrencia)
        {
            var loc = ocorrencia.Localizacao;
            var sol = ocorrencia.Solicitante;
            var av  = ocorrencia.AvaliacaoRisco;
            // Usa o agendamento mais recente (maior Numero) para preencher vistoriadores
            var ag  = ocorrencia.Agendamentos.OrderByDescending(a => a.Numero).FirstOrDefault();

            return new Dictionary<string, string>
            {
                // Dados da ocorrência
                { "PROTOCOLO",      ocorrencia.Protocolo },
                { "STATUS",         ocorrencia.Status.ToString() },
                { "DATA_ABERTURA",  ocorrencia.AbertaEm.ToString("dd/MM/yyyy HH:mm") },
                { "DESCRICAO",      ocorrencia.DescricaoProblema },

                // Classificação de risco (Etapa 2 — pode estar vazia)
                { "TIPO_RISCO",      av?.TipificacaoInicial.ToString() ?? "Não classificado" },
                { "GRAU_RISCO",      av?.GrauRiscoInicial.ToString()   ?? "Não avaliado" },
                { "EMERGENCIA",      av?.Emergencia == true ? "Sim" : "Não" },

                // Dados do solicitante (Etapa 1)
                { "CIDADAO_NOME",     sol?.Nome     ?? "" },
                { "CIDADAO_CPF",      sol?.Cpf      ?? "" },
                { "CIDADAO_RG",       sol?.Rg       ?? "" },
                { "CIDADAO_EMAIL",    sol?.Email    ?? "" },
                { "CIDADAO_TELEFONE", sol?.Telefone ?? "" },
                { "CIDADAO_CELULAR",  sol?.Celular  ?? "" },

                // Endereço (Etapa 1 — Localizacao)
                { "ENDERECO",   loc?.Endereco  ?? "" },
                { "BAIRRO",     loc?.Bairro    ?? "" },
                { "CIDADE",     loc?.Cidade    ?? "" },
                { "UF",         loc?.Uf        ?? "" },
                { "CEP",        loc?.Cep       ?? "" },
                { "COORDENADA", loc?.Coordenada ?? "" },

                // Vistoriadores designados (Etapa 3 — pode estar vazia)
                { "VISTORIADOR_1", ag?.Vistoriador1.Nome ?? "Não atribuído" },
                { "VISTORIADOR_2", ag?.Vistoriador2?.Nome ?? "Não atribuído" },

                // Datas calculadas a partir do status atual (usa a vistoria mais recente se houver)
                { "DATA_VISTORIA",  ocorrencia.Vistorias.OrderByDescending(v => v.Numero).FirstOrDefault()?.DataVistoria.ToString("dd/MM/yyyy") ?? "Pendente" },
                { "DATA_ENCERRAMENTO", ocorrencia.EncaminhamentoFinal?.RegistradoEm.ToString("dd/MM/yyyy HH:mm") ?? "Em andamento" }
            };
        }

        /// <summary>
        /// Mescla dados customizados com dados padrão da ocorrência
        /// </summary>
        public static Dictionary<string, string> MesclarComDadosCustomizados(
            this Ocorrencia ocorrencia,
            Dictionary<string, string>? dadosAdicionais = null)
        {
            var dadosBase = ocorrencia.ParaDicionarioTemplate();

            if (dadosAdicionais != null)
            {
                foreach (var kvp in dadosAdicionais)
                {
                    dadosBase[kvp.Key] = kvp.Value; // Sobrescreve ou adiciona
                }
            }

            return dadosBase;
        }
    }
}
