using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class TipificacaoInicialMultivalorada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As views de BI leem esta coluna, e o PostgreSQL recusa alterar o
            // tipo de uma coluna usada por uma view. Derrubamos todas antes; o
            // ViewsBiSeeder as recria na inicialização seguinte da API.
            migrationBuilder.Sql("""
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

            // O scaffold do EF gera um ALTER COLUMN sem USING, que o PostgreSQL
            // recusa em tabela com dados ("column cannot be cast automatically
            // to type text[]"). Foi assim que a conversão integer[]→text[] travou
            // o deploy anterior. Aqui a conversão é explícita: o valor único vira
            // um array de um elemento, e vazio/nulo vira array vazio.
            migrationBuilder.Sql("""
                ALTER TABLE avaliacoes_risco
                ALTER COLUMN "TipificacaoInicial" TYPE text[]
                USING (
                    CASE
                        WHEN "TipificacaoInicial" IS NULL
                          OR btrim("TipificacaoInicial") = '' THEN ARRAY[]::text[]
                        ELSE ARRAY["TipificacaoInicial"]
                    END
                );
                """);

            migrationBuilder.Sql("""
                ALTER TABLE avaliacoes_risco
                ALTER COLUMN "TipificacaoInicial" SET DEFAULT ARRAY[]::text[];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversão com perda: só a primeira tipificação sobrevive.
            migrationBuilder.Sql("""
                ALTER TABLE avaliacoes_risco
                ALTER COLUMN "TipificacaoInicial" DROP DEFAULT;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE avaliacoes_risco
                ALTER COLUMN "TipificacaoInicial" TYPE text
                USING coalesce("TipificacaoInicial"[1], '');
                """);
        }
    }
}
