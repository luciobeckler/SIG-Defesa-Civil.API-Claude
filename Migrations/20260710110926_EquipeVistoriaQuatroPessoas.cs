using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class EquipeVistoriaQuatroPessoas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Vistoriador3Id",
                table: "vistorias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Vistoriador4Id",
                table: "vistorias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Vistoriador3Id",
                table: "agendamentos_vistoria",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Vistoriador4Id",
                table: "agendamentos_vistoria",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_Vistoriador3Id",
                table: "vistorias",
                column: "Vistoriador3Id");

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_Vistoriador4Id",
                table: "vistorias",
                column: "Vistoriador4Id");

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_vistoria_Vistoriador3Id",
                table: "agendamentos_vistoria",
                column: "Vistoriador3Id");

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_vistoria_Vistoriador4Id",
                table: "agendamentos_vistoria",
                column: "Vistoriador4Id");

            migrationBuilder.AddForeignKey(
                name: "FK_agendamentos_vistoria_usuarios_Vistoriador3Id",
                table: "agendamentos_vistoria",
                column: "Vistoriador3Id",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_agendamentos_vistoria_usuarios_Vistoriador4Id",
                table: "agendamentos_vistoria",
                column: "Vistoriador4Id",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_vistorias_usuarios_Vistoriador3Id",
                table: "vistorias",
                column: "Vistoriador3Id",
                principalTable: "usuarios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_vistorias_usuarios_Vistoriador4Id",
                table: "vistorias",
                column: "Vistoriador4Id",
                principalTable: "usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agendamentos_vistoria_usuarios_Vistoriador3Id",
                table: "agendamentos_vistoria");

            migrationBuilder.DropForeignKey(
                name: "FK_agendamentos_vistoria_usuarios_Vistoriador4Id",
                table: "agendamentos_vistoria");

            migrationBuilder.DropForeignKey(
                name: "FK_vistorias_usuarios_Vistoriador3Id",
                table: "vistorias");

            migrationBuilder.DropForeignKey(
                name: "FK_vistorias_usuarios_Vistoriador4Id",
                table: "vistorias");

            migrationBuilder.DropIndex(
                name: "IX_vistorias_Vistoriador3Id",
                table: "vistorias");

            migrationBuilder.DropIndex(
                name: "IX_vistorias_Vistoriador4Id",
                table: "vistorias");

            migrationBuilder.DropIndex(
                name: "IX_agendamentos_vistoria_Vistoriador3Id",
                table: "agendamentos_vistoria");

            migrationBuilder.DropIndex(
                name: "IX_agendamentos_vistoria_Vistoriador4Id",
                table: "agendamentos_vistoria");

            migrationBuilder.DropColumn(
                name: "Vistoriador3Id",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "Vistoriador4Id",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "Vistoriador3Id",
                table: "agendamentos_vistoria");

            migrationBuilder.DropColumn(
                name: "Vistoriador4Id",
                table: "agendamentos_vistoria");
        }
    }
}
