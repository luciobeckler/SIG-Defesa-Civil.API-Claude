namespace SIG_Defesa_Civil.API.Infrastructure.Seeders
{
    using System.Reflection;
    using Microsoft.EntityFrameworkCore;
    using SIG_Defesa_Civil.API.Data.Models;

    /// <summary>
    /// Aplica a camada de BI (<c>Scripts/BI/views_bi.sql</c>) no banco.
    ///
    /// O arquivo SQL é a fonte única da verdade e viaja embutido no assembly
    /// (EmbeddedResource no .csproj), então não depende de COPY no Dockerfile.
    /// Como todo o script é CREATE OR REPLACE, rodar a cada inicialização é
    /// seguro e mantém as views sempre em dia com o que está no repositório —
    /// sem precisar de uma migration nova a cada ajuste de indicador.
    /// </summary>
    public static class ViewsBiSeeder
    {
        private const string RecursoSql = "SIG_Defesa_Civil.API.Scripts.BI.views_bi.sql";

        /// <summary>
        /// Derruba todas as views de BI. Chamado ANTES das migrations.
        ///
        /// O PostgreSQL recusa alterar o tipo de uma coluna usada por uma view
        /// ("não é possível alterar o tipo de dados de uma coluna usada por uma
        /// visão"). Sem isto, qualquer migration que mexa numa coluna lida pelo
        /// BI falharia. Como <see cref="SeedAsync"/> roda logo depois e recria
        /// tudo a partir do arquivo, derrubar aqui não custa nada.
        /// </summary>
        public static async Task RemoverAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DefesaCivilContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ViewsBiSeeder));

            try
            {
                await context.Database.ExecuteSqlRawAsync("""
                    DO $$
                    DECLARE v record;
                    BEGIN
                        FOR v IN SELECT viewname FROM pg_views
                                 WHERE schemaname = 'public' AND viewname LIKE 'vw\_bi\_%'
                        LOOP
                            EXECUTE format('DROP VIEW IF EXISTS %I CASCADE', v.viewname);
                        END LOOP;
                    END $$;
                    """);
            }
            catch (Exception ex)
            {
                // Banco ainda não criado na primeira execução — nada a derrubar.
                logger.LogDebug(ex, "Não foi possível remover as views de BI (provavelmente ainda não existem).");
            }
        }

        /// <summary>
        /// Executa o script das views. Chamado na inicialização, depois das migrations.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DefesaCivilContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ViewsBiSeeder));

            string sql;
            try
            {
                sql = await LerRecursoAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Não foi possível ler o script das views de BI.");
                return;
            }

            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
                logger.LogInformation("Views de BI aplicadas.");
            }
            catch (Exception ex)
            {
                // Falha aqui não pode derrubar a API: as views servem relatórios,
                // não o fluxo de atendimento. O erro fica registrado para correção.
                logger.LogError(ex, "Falha ao aplicar as views de BI. A API segue no ar sem elas.");
            }
        }

        private static async Task<string> LerRecursoAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            await using var stream = assembly.GetManifestResourceStream(RecursoSql)
                ?? throw new InvalidOperationException(
                    $"Recurso '{RecursoSql}' não encontrado no assembly. " +
                    "Confira o EmbeddedResource de Scripts/BI/views_bi.sql no .csproj.");

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }
}
