using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class NotificadosPropriedadeDaOcorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntregaRelatorio",
                table: "encaminhamentos_finais");

            migrationBuilder.AddColumn<string>(
                name: "FormaRecebimento",
                table: "notificados",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormaRecebimento",
                table: "notificados");

            migrationBuilder.AddColumn<string>(
                name: "EntregaRelatorio",
                table: "encaminhamentos_finais",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
