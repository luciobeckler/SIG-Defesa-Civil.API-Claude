namespace SIG_Defesa_Civil.API.Data.DTO.Requests
{
    /// <summary>
    /// Parâmetros de paginação
    /// </summary>
    public class PaginacaoDto
    {
        private int _paginaAtual = 1;
        private int _itensPorPagina = 20;

        public int PaginaAtual
        {
            get => _paginaAtual;
            set => _paginaAtual = value < 1 ? 1 : value;
        }

        public int ItensPorPagina
        {
            get => _itensPorPagina;
            set => _itensPorPagina = value switch
            {
                < 1 => 10,
                > 100 => 100,
                _ => value
            };
        }

        public int Skip => (PaginaAtual - 1) * ItensPorPagina;
        public int Take => ItensPorPagina;
    }

}
