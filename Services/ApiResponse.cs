using SIG_Defesa_Civil.API.Enums;

namespace SIG_Defesa_Civil.API.Services
{
    /// <summary>
    /// Envelope padrão para todas as respostas da API
    /// </summary>
    /// <typeparam name="T">Tipo do payload de dados</typeparam>
    public class ApiResponse<T>
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public T? Dados { get; set; }
        public ErrosRequisicoes? CodigoErro { get; set; }

        public static ApiResponse<T> Success(T dados, string mensagem = "Operação realizada com sucesso")
        {
            return new ApiResponse<T>
            {
                Sucesso = true,
                Mensagem = mensagem,
                Dados = dados
            };
        }

        public static ApiResponse<T> Error(string mensagem, ErrosRequisicoes? codigoErro = null)
        {
            return new ApiResponse<T>
            {
                Sucesso = false,
                Mensagem = mensagem,
                CodigoErro = codigoErro
            };
        }
    }
}
