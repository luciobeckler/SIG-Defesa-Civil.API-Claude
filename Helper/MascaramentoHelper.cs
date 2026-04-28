namespace SIG_Defesa_Civil.API.Helper
{
    /// <summary>
    /// Utilitário para mascaramento de dados sensíveis (LGPD)
    /// </summary>
    public static class MascaramentoHelper
    {
        /// <summary>
        /// Mascara CPF: "12345678901" -> "***.***.***-01"
        /// Exibe apenas os dois últimos dígitos
        /// </summary>
        public static string MascararCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11)
                return "***.***.***-**";

            return $"***.***.***.{cpf.Substring(9, 2)}";
        }

        /// <summary>
        /// Mascara nome: "João da Silva Santos" -> "João d* S***** S*****"
        /// Exibe primeiro nome completo, demais com inicial + asteriscos
        /// </summary>
        public static string MascararNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return "***";

            var partes = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 1)
                return partes[0]; // Nome único, não mascara

            var resultado = new List<string> { partes[0] }; // Primeiro nome completo

            for (int i = 1; i < partes.Length; i++)
            {
                var parte = partes[i];
                if (parte.Length <= 2)
                {
                    // Preposições/conectivos curtos (da, de, dos, etc)
                    resultado.Add(parte.Substring(0, 1) + "*");
                }
                else
                {
                    // Sobrenomes: primeira letra + asteriscos
                    resultado.Add(parte.Substring(0, 1) + new string('*', parte.Length - 1));
                }
            }

            return string.Join(" ", resultado);
        }

        /// <summary>
        /// Mascara email: "joao.silva@email.com" -> "j***@email.com"
        /// Exibe primeira letra do usuário + domínio completo
        /// </summary>
        public static string MascararEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "***@***.***";

            var partes = email.Split('@');
            var usuario = partes[0];
            var dominio = partes[1];

            if (usuario.Length <= 1)
                return $"{usuario}***@{dominio}";

            return $"{usuario.Substring(0, 1)}***@{dominio}";
        }

        /// <summary>
        /// Mascara telefone: "+5531987654321" -> "+55 31 *****-4321"
        /// Exibe código país, DDD e últimos 4 dígitos
        /// </summary>
        public static string MascararTelefone(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return "***";

            // Remove caracteres não numéricos
            var apenasNumeros = new string(telefone.Where(char.IsDigit).ToArray());

            if (apenasNumeros.Length < 10)
                return "*****-****";

            // Formato esperado: +55 31 98765-4321 (13 dígitos com +55)
            if (apenasNumeros.Length == 13)
            {
                var codigoPais = apenasNumeros.Substring(0, 2);  // 55
                var ddd = apenasNumeros.Substring(2, 2);         // 31
                var final = apenasNumeros.Substring(9, 4);       // 4321

                return $"+{codigoPais} {ddd} *****-{final}";
            }

            // Formato Brasil sem código país: 11 dígitos
            if (apenasNumeros.Length == 11)
            {
                var ddd = apenasNumeros.Substring(0, 2);
                var final = apenasNumeros.Substring(7, 4);

                return $"{ddd} *****-{final}";
            }

            // Formato antigo: 10 dígitos
            if (apenasNumeros.Length == 10)
            {
                var ddd = apenasNumeros.Substring(0, 2);
                var final = apenasNumeros.Substring(6, 4);

                return $"{ddd} ****-{final}";
            }

            // Fallback genérico
            var ultimosDigitos = apenasNumeros.Substring(Math.Max(0, apenasNumeros.Length - 4));
            return $"*****-{ultimosDigitos}";
        }
    }
}