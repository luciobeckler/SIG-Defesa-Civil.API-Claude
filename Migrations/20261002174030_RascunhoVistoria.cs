using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class RascunhoVistoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rascunhos_vistoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    ConteudoJson = table.Column<string>(type: "jsonb", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rascunhos_vistoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rascunhos_vistoria_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rascunhos_vistoria_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rascunhos_vistoria_OcorrenciaId_UsuarioId",
                table: "rascunhos_vistoria",
                columns: new[] { "OcorrenciaId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rascunhos_vistoria_UsuarioId",
                table: "rascunhos_vistoria",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rascunhos_vistoria");
        }
    }
}
