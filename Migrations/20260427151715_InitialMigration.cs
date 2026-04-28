using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "seq_protocolo_ano");

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Cpf = table.Column<string>(type: "text", nullable: true),
                    Rg = table.Column<string>(type: "text", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    TipoUsuario = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ocorrencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Protocolo = table.Column<string>(type: "text", nullable: false),
                    CidadaoId = table.Column<int>(type: "integer", nullable: false),
                    EnderecoCompleto = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    TipoRisco = table.Column<string>(type: "text", nullable: true),
                    NivelGravidade = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AtendenteId = table.Column<int>(type: "integer", nullable: true),
                    VistoriadorId = table.Column<int>(type: "integer", nullable: true),
                    AbertaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriagemEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VistoriaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcluidaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ocorrencias_usuarios_AtendenteId",
                        column: x => x.AtendenteId,
                        principalTable: "usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ocorrencias_usuarios_CidadaoId",
                        column: x => x.CidadaoId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ocorrencias_usuarios_VistoriadorId",
                        column: x => x.VistoriadorId,
                        principalTable: "usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "arquivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    NomeOriginal = table.Column<string>(type: "text", nullable: false),
                    TipoArquivo = table.Column<string>(type: "text", nullable: false),
                    SharepointId = table.Column<string>(type: "text", nullable: false),
                    SharepointUrl = table.Column<string>(type: "text", nullable: false),
                    EnviadoPor = table.Column<int>(type: "integer", nullable: false),
                    EnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioEnvioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arquivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_arquivos_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_arquivos_usuarios_UsuarioEnvioId",
                        column: x => x.UsuarioEnvioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "log_acesso_lgpd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: true),
                    ArquivoId = table.Column<int>(type: "integer", nullable: true),
                    Acao = table.Column<string>(type: "text", nullable: false),
                    IpOrigem = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_acesso_lgpd", x => x.Id);
                    table.ForeignKey(
                        name: "FK_log_acesso_lgpd_arquivos_ArquivoId",
                        column: x => x.ArquivoId,
                        principalTable: "arquivos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_log_acesso_lgpd_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_log_acesso_lgpd_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_OcorrenciaId",
                table: "arquivos",
                column: "OcorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_UsuarioEnvioId",
                table: "arquivos",
                column: "UsuarioEnvioId");

            migrationBuilder.CreateIndex(
                name: "IX_log_acesso_lgpd_ArquivoId",
                table: "log_acesso_lgpd",
                column: "ArquivoId");

            migrationBuilder.CreateIndex(
                name: "IX_log_acesso_lgpd_OcorrenciaId",
                table: "log_acesso_lgpd",
                column: "OcorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_log_acesso_lgpd_UsuarioId",
                table: "log_acesso_lgpd",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_AtendenteId",
                table: "ocorrencias",
                column: "AtendenteId");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_CidadaoId",
                table: "ocorrencias",
                column: "CidadaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_Protocolo",
                table: "ocorrencias",
                column: "Protocolo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_VistoriadorId",
                table: "ocorrencias",
                column: "VistoriadorId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Cpf",
                table: "usuarios",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_acesso_lgpd");

            migrationBuilder.DropTable(
                name: "arquivos");

            migrationBuilder.DropTable(
                name: "ocorrencias");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropSequence(
                name: "seq_protocolo_ano");
        }
    }
}
