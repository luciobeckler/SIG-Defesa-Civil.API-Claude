namespace SIG_Defesa_Civil.API.Infrastructure.Seeders
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;
    using SIG_Defesa_Civil.API.Data.Configuration.Auth;
    using SIG_Defesa_Civil.API.Data.Models;
    using SIG_Defesa_Civil.API.Data.Models.Tabelas;
    using SIG_Defesa_Civil.API.Enums;

    public static class AdminSeeder
    {
        /// <summary>
        /// Cria o primeiro usuário ADMIN se nenhum existir no banco.
        /// Chamado uma vez na inicialização da aplicação.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DefesaCivilContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
            var settings = scope.ServiceProvider
                .GetRequiredService<IOptions<AdminSeedSettings>>().Value;

            // Aplica migrations pendentes automaticamente em desenvolvimento
            await context.Database.MigrateAsync();

            if (await context.Usuarios.AnyAsync(u => u.TipoUsuario == TipoUsuario.ADMIN))
                return;

            var admin = new Usuario
            {
                Nome = settings.Nome,
                Email = settings.Email,
                TipoUsuario = TipoUsuario.ADMIN,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            };

            admin.SenhaHash = hasher.HashPassword(admin, settings.Senha);

            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();

            Console.WriteLine($"[AdminSeeder] Usuário admin criado: {settings.Email}");
        }
    }
}
