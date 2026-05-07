using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class AgendamentosAndVistoriasOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vistorias_OcorrenciaId",
                table: "vistorias");

            migrationBuilder.DropIndex(
                name: "IX_agendamentos_vistoria_OcorrenciaId",
                table: "agendamentos_vistoria");

            migrationBuilder.AddColumn<int>(
                name: "AgendamentoId",
                table: "vistorias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Numero",
                table: "vistorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Numero",
                table: "agendamentos_vistoria",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "agendamentos_vistoria",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_AgendamentoId",
                table: "vistorias",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_OcorrenciaId_Numero",
                table: "vistorias",
                columns: new[] { "OcorrenciaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_vistoria_OcorrenciaId_Numero",
                table: "agendamentos_vistoria",
                columns: new[] { "OcorrenciaId", "Numero" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_vistorias_agendamentos_vistoria_AgendamentoId",
                table: "vistorias",
                column: "AgendamentoId",
                principalTable: "agendamentos_vistoria",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vistorias_agendamentos_vistoria_AgendamentoId",
                table: "vistorias");

            migrationBuilder.DropIndex(
                name: "IX_vistorias_AgendamentoId",
                table: "vistorias");

            migrationBuilder.DropIndex(
                name: "IX_vistorias_OcorrenciaId_Numero",
                table: "vistorias");

            migrationBuilder.DropIndex(
                name: "IX_agendamentos_vistoria_OcorrenciaId_Numero",
                table: "agendamentos_vistoria");

            migrationBuilder.DropColumn(
                name: "AgendamentoId",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "agendamentos_vistoria");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "agendamentos_vistoria");

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_OcorrenciaId",
                table: "vistorias",
                column: "OcorrenciaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_vistoria_OcorrenciaId",
                table: "agendamentos_vistoria",
                column: "OcorrenciaId",
                unique: true);
        }
    }
}
