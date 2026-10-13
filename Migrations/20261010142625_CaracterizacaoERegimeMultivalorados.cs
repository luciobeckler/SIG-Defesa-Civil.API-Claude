using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class CaracterizacaoERegimeMultivalorados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O EF escreve "ALTER COLUMN ... TYPE text[]" sem USING, e o PostgreSQL
            // recusa isso em tabela com dados: não existe conversão automática de
            // text para text[]. Escrito à mão para converter preservando o que há —
            // cada valor atual vira um array de um elemento, e vazio/nulo vira '{}'.
            migrationBuilder.Sql(@"
                ALTER TABLE vistorias
                ALTER COLUMN ""CaracterizacaoDoLocal"" TYPE text[]
                USING CASE
                    WHEN ""CaracterizacaoDoLocal"" IS NULL
                      OR btrim(""CaracterizacaoDoLocal"") = '' THEN '{}'::text[]
                    ELSE ARRAY[""CaracterizacaoDoLocal""]
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE vistorias
                ALTER COLUMN ""CaracterizacaoDoLocal"" SET DEFAULT '{}'::text[],
                ALTER COLUMN ""CaracterizacaoDoLocal"" SET NOT NULL;");

            migrationBuilder.Sql(@"
                ALTER TABLE vistorias
                ALTER COLUMN ""RegimeOcupacao"" TYPE text[]
                USING CASE
                    WHEN ""RegimeOcupacao"" IS NULL
                      OR btrim(""RegimeOcupacao"") = '' THEN '{}'::text[]
                    ELSE ARRAY[""RegimeOcupacao""]
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE vistorias
                ALTER COLUMN ""RegimeOcupacao"" SET DEFAULT '{}'::text[],
                ALTER COLUMN ""RegimeOcupacao"" SET NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Volta para texto único ficando com o primeiro valor: se alguém tiver
            // marcado mais de um, os demais se perdem — é o custo de desfazer.
            migrationBuilder.Sql(@"
                ALTER TABLE vistorias
                ALTER COLUMN ""CaracterizacaoDoLocal"" DROP DEFAULT,
                ALTER COLUMN ""CaracterizacaoDoLocal"" TYPE text
                USING coalesce(""CaracterizacaoDoLocal""[1], ''),
                ALTER COLUMN ""CaracterizacaoDoLocal"" DROP NOT NULL;");

            migrationBuilder.Sql(@"
                ALTER TABLE vistorias
                ALTER COLUMN ""RegimeOcupacao"" DROP DEFAULT,
                ALTER COLUMN ""RegimeOcupacao"" TYPE text
                USING coalesce(""RegimeOcupacao""[1], '');");
        }
    }
}
