using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class SolicitanteEmbutidoNaOcorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_arquivos_usuarios_enviado_por",
                table: "arquivos");

            migrationBuilder.DropForeignKey(
                name: "FK_log_acesso_lgpd_usuarios_UsuarioId",
                table: "log_acesso_lgpd");

            migrationBuilder.DropForeignKey(
                name: "FK_ocorrencias_usuarios_CriadoPorId",
                table: "ocorrencias");

            migrationBuilder.AlterColumn<int>(
                name: "CriadoPorId",
                table: "ocorrencias",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteCelular",
                table: "ocorrencias",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteCpf",
                table: "ocorrencias",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteEmail",
                table: "ocorrencias",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteNome",
                table: "ocorrencias",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteOrgaoEmissor",
                table: "ocorrencias",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteRg",
                table: "ocorrencias",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteTelefone",
                table: "ocorrencias",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "log_acesso_lgpd",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "enviado_por",
                table: "arquivos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // ─────────────────────────────────────────────────────────────────────
            // Migração dos dados: usuarios(CIDADAO) → colunas da própria ocorrência.
            // Precisa rodar ANTES de derrubar a FK/coluna SolicitanteId.
            // ─────────────────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                UPDATE ocorrencias o
                SET "SolicitanteNome"         = u."Nome",
                    "SolicitanteCpf"          = NULLIF(regexp_replace(COALESCE(u."Cpf", ''), '\D', '', 'g'), ''),
                    "SolicitanteRg"           = u."Rg",
                    "SolicitanteOrgaoEmissor" = u."OrgaoEmissor",
                    "SolicitanteEmail"        = u."Email",
                    "SolicitanteTelefone"     = u."Telefone",
                    "SolicitanteCelular"      = u."Celular"
                FROM usuarios u
                WHERE u."Id" = o."SolicitanteId";
                """);

            // Só então a coluna/FK antiga pode sair
            migrationBuilder.DropForeignKey(
                name: "FK_ocorrencias_usuarios_SolicitanteId",
                table: "ocorrencias");

            migrationBuilder.DropIndex(
                name: "IX_ocorrencias_SolicitanteId",
                table: "ocorrencias");

            migrationBuilder.DropColumn(
                name: "SolicitanteId",
                table: "ocorrencias");

            // Cidadãos não são usuários do sistema — as contas criadas por engano
            // na abertura de ocorrências deixam de existir.
            migrationBuilder.Sql("""DELETE FROM usuarios WHERE "TipoUsuario" = 'CIDADAO';""");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_SolicitanteCpf",
                table: "ocorrencias",
                column: "SolicitanteCpf");

            migrationBuilder.AddForeignKey(
                name: "FK_arquivos_usuarios_enviado_por",
                table: "arquivos",
                column: "enviado_por",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_log_acesso_lgpd_usuarios_UsuarioId",
                table: "log_acesso_lgpd",
                column: "UsuarioId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ocorrencias_usuarios_CriadoPorId",
                table: "ocorrencias",
                column: "CriadoPorId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <summary>
        /// Reversão apenas estrutural: as contas CIDADAO apagadas no Up não voltam,
        /// então a FK SolicitanteId não tem para onde apontar. Restaure de um backup
        /// em vez de reverter esta migration.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_arquivos_usuarios_enviado_por",
                table: "arquivos");

            migrationBuilder.DropForeignKey(
                name: "FK_log_acesso_lgpd_usuarios_UsuarioId",
                table: "log_acesso_lgpd");

            migrationBuilder.DropForeignKey(
                name: "FK_ocorrencias_usuarios_CriadoPorId",
                table: "ocorrencias");

            migrationBuilder.DropIndex(
                name: "IX_ocorrencias_SolicitanteCpf",
                table: "ocorrencias");

            migrationBuilder.DropColumn(
                name: "SolicitanteCelular",
                table: "ocorrencias");

            migrationBuilder.DropColumn(
                name: "SolicitanteCpf",
                table: "ocorrencias");

            migrationBuilder.DropColumn(
                name: "SolicitanteEmail",
                table: "ocorrencias");

            migrationBuilder.DropColumn(
                name: "SolicitanteNome",
                table: "ocorrencias");

            migrationBuilder.DropColumn(
                name: "SolicitanteOrgaoEmissor",
                table: "ocorrencias");

            migrationBuilder.DropColumn(
                name: "SolicitanteRg",
                table: "ocorrencias");

            migrationBuilder.DropColumn(
                name: "SolicitanteTelefone",
                table: "ocorrencias");

            migrationBuilder.AlterColumn<int>(
                name: "CriadoPorId",
                table: "ocorrencias",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SolicitanteId",
                table: "ocorrencias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "log_acesso_lgpd",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "enviado_por",
                table: "arquivos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_SolicitanteId",
                table: "ocorrencias",
                column: "SolicitanteId");

            migrationBuilder.AddForeignKey(
                name: "FK_arquivos_usuarios_enviado_por",
                table: "arquivos",
                column: "enviado_por",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_log_acesso_lgpd_usuarios_UsuarioId",
                table: "log_acesso_lgpd",
                column: "UsuarioId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ocorrencias_usuarios_CriadoPorId",
                table: "ocorrencias",
                column: "CriadoPorId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ocorrencias_usuarios_SolicitanteId",
                table: "ocorrencias",
                column: "SolicitanteId",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
