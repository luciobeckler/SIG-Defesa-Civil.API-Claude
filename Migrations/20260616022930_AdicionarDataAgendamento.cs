using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDataAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Data",
                table: "agendamentos_vistoria",
                type: "date",
                nullable: true);

            // Backfill: preenche a Data dos agendamentos existentes a partir da
            // primeira tentativa registrada, para que apareçam no calendário.
            migrationBuilder.Sql(@"
                UPDATE agendamentos_vistoria a
                SET ""Data"" = sub.dt
                FROM (
                    SELECT ""AgendamentoId"", MIN(""DataHoraTentativa"")::date AS dt
                    FROM tentativas_vistoria
                    GROUP BY ""AgendamentoId""
                ) sub
                WHERE a.""Id"" = sub.""AgendamentoId"" AND a.""Data"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "agendamentos_vistoria");
        }
    }
}
