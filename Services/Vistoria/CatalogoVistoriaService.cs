using Microsoft.EntityFrameworkCore;
using SIG_Defesa_Civil.API.Data.DTO.Requests.Ocorrencias;
using SIG_Defesa_Civil.API.Data.DTO.Responses.Ocorrencias;
using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
using SIG_Defesa_Civil.API.Data.Models;

namespace SIG_Defesa_Civil.API.Services.Vistoria
{
    public class CatalogoVistoriaService : ICatalogoVistoriaService
    {
        private readonly DefesaCivilContext _context;
        private readonly ILogger<CatalogoVistoriaService> _logger;

        public CatalogoVistoriaService(
            DefesaCivilContext context,
            ILogger<CatalogoVistoriaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<OpcaoCampoVistoriaDto>> ListarAsync()
        {
            return await _context.OpcoesCampoVistoria
                .OrderBy(o => o.Campo).ThenBy(o => o.Label)
                .Select(o => new OpcaoCampoVistoriaDto
                {
                    Campo = o.Campo,
                    Valor = o.Valor,
                    Label = o.Label,
                })
                .ToListAsync();
        }

        public async Task<OpcaoCampoVistoriaDto> AdicionarAsync(CriarOpcaoCampoRequest request)
        {
            var campo = (request.Campo ?? string.Empty).Trim().ToUpperInvariant();
            var label = (request.Valor ?? string.Empty).Trim();

            if (!CamposVistoria.Todos.Contains(campo))
                throw new InvalidOperationException($"Campo '{request.Campo}' inválido para opções personalizadas.");

            if (string.IsNullOrWhiteSpace(label))
                throw new InvalidOperationException("A nova opção não pode ser vazia.");

            // Valor armazenado = o próprio texto da opção (consistente com o que a vistoria grava)
            var valor = label;

            var existente = await _context.OpcoesCampoVistoria
                .FirstOrDefaultAsync(o => o.Campo == campo && o.Valor == valor);

            if (existente != null)
                return new OpcaoCampoVistoriaDto { Campo = existente.Campo, Valor = existente.Valor, Label = existente.Label };

            var opcao = new OpcaoCampoVistoria
            {
                Campo = campo,
                Valor = valor,
                Label = label,
                CriadoEm = DateTime.UtcNow,
            };

            _context.OpcoesCampoVistoria.Add(opcao);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Opção personalizada '{Valor}' adicionada ao campo {Campo}", valor, campo);

            return new OpcaoCampoVistoriaDto { Campo = opcao.Campo, Valor = opcao.Valor, Label = opcao.Label };
        }
    }
}
