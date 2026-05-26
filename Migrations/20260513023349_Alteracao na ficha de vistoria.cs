using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG_Defesa_Civil.API.Migrations
{
    /// <inheritdoc />
    public partial class Alteracaonafichadevistoria : Migration
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
                    OrgaoEmissor = table.Column<string>(type: "text", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    Celular = table.Column<string>(type: "text", nullable: true),
                    Matricula = table.Column<string>(type: "text", nullable: true),
                    SenhaHash = table.Column<string>(type: "text", nullable: true),
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
                    SolicitanteId = table.Column<int>(type: "integer", nullable: false),
                    DescricaoProblema = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CriadoPorId = table.Column<int>(type: "integer", nullable: false),
                    AbertaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ocorrencias_usuarios_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ocorrencias_usuarios_ExcluidoPorId",
                        column: x => x.ExcluidoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ocorrencias_usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agendamentos_vistoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Vistoriador1Id = table.Column<int>(type: "integer", nullable: false),
                    Vistoriador2Id = table.Column<int>(type: "integer", nullable: true),
                    AgendadoPorId = table.Column<int>(type: "integer", nullable: false),
                    AgendadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agendamentos_vistoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agendamentos_vistoria_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agendamentos_vistoria_usuarios_AgendadoPorId",
                        column: x => x.AgendadoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agendamentos_vistoria_usuarios_Vistoriador1Id",
                        column: x => x.Vistoriador1Id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agendamentos_vistoria_usuarios_Vistoriador2Id",
                        column: x => x.Vistoriador2Id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "arquivos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ocorrencia_id = table.Column<int>(type: "integer", nullable: false),
                    nome_original = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo_arquivo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    caminho_relativo = table.Column<string>(type: "text", nullable: false),
                    tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                    enviado_por = table.Column<int>(type: "integer", nullable: false),
                    enviado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arquivos", x => x.id);
                    table.ForeignKey(
                        name: "FK_arquivos_ocorrencias_ocorrencia_id",
                        column: x => x.ocorrencia_id,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_arquivos_usuarios_enviado_por",
                        column: x => x.enviado_por,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "avaliacoes_risco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    TipificacaoInicial = table.Column<string>(type: "text", nullable: false),
                    GrauRiscoInicial = table.Column<string>(type: "text", nullable: false),
                    AbertaPorUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    RequisicaoSetorDocumento = table.Column<string>(type: "text", nullable: true),
                    Emergencia = table.Column<bool>(type: "boolean", nullable: false),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avaliacoes_risco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_avaliacoes_risco_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_avaliacoes_risco_usuarios_AbertaPorUsuarioId",
                        column: x => x.AbertaPorUsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "localizacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    Endereco = table.Column<string>(type: "text", nullable: false),
                    Bairro = table.Column<string>(type: "text", nullable: false),
                    Numero = table.Column<string>(type: "text", nullable: true),
                    Cep = table.Column<string>(type: "text", nullable: true),
                    Complemento = table.Column<string>(type: "text", nullable: true),
                    Cidade = table.Column<string>(type: "text", nullable: false),
                    Uf = table.Column<string>(type: "text", nullable: false),
                    Coordenada = table.Column<string>(type: "text", nullable: true),
                    Referencia = table.Column<string>(type: "text", nullable: true),
                    NumeroIptu = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_localizacoes_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notificados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    RgCpf = table.Column<string>(type: "text", nullable: true),
                    DataNotificacao = table.Column<DateOnly>(type: "date", nullable: false),
                    RegistradoPorId = table.Column<int>(type: "integer", nullable: false),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notificados_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notificados_usuarios_RegistradoPorId",
                        column: x => x.RegistradoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Observacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observacoes_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Observacoes_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tentativas_vistoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgendamentoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroTentativa = table.Column<int>(type: "integer", nullable: false),
                    DataHoraTentativa = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tentativas_vistoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tentativas_vistoria_agendamentos_vistoria_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "agendamentos_vistoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vistorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    AgendamentoId = table.Column<int>(type: "integer", nullable: true),
                    DataVistoria = table.Column<DateOnly>(type: "date", nullable: false),
                    HorarioInicio = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HorarioTermino = table.Column<TimeSpan>(type: "interval", nullable: false),
                    DescricaoDoLocal = table.Column<string>(type: "text", nullable: true),
                    CaracterizacaoDoLocal = table.Column<int>(type: "integer", nullable: true),
                    Edificacao = table.Column<string>(type: "text", nullable: false),
                    Estrutura = table.Column<string>(type: "text", nullable: false),
                    NumeroMoradias = table.Column<int>(type: "integer", nullable: false),
                    NumeroComodos = table.Column<int>(type: "integer", nullable: false),
                    NumeroPavimentos = table.Column<int>(type: "integer", nullable: false),
                    NumeroMoradiasNoLote = table.Column<int>(type: "integer", nullable: false),
                    PossuiUnidadeFamiliar = table.Column<bool>(type: "boolean", nullable: false),
                    NumeroAdultos = table.Column<int>(type: "integer", nullable: false),
                    NumeroCriancas = table.Column<int>(type: "integer", nullable: false),
                    NumeroIdosos = table.Column<int>(type: "integer", nullable: false),
                    NumeroDeficientes = table.Column<int>(type: "integer", nullable: false),
                    TotalMoradores = table.Column<int>(type: "integer", nullable: false),
                    TipoRisco = table.Column<string>(type: "text", nullable: false),
                    GrauRiscoEncontrado = table.Column<string>(type: "text", nullable: false),
                    TipificacaoOcorrencia = table.Column<int[]>(type: "integer[]", nullable: false),
                    RegimeOcupacao = table.Column<string>(type: "text", nullable: false),
                    Motivacao = table.Column<int[]>(type: "integer[]", nullable: false),
                    AreasAfetadas = table.Column<int[]>(type: "integer[]", nullable: false),
                    Interdicao = table.Column<string>(type: "text", nullable: false),
                    Remocao = table.Column<string>(type: "text", nullable: false),
                    Orientacoes = table.Column<int[]>(type: "integer[]", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    EncaminhamentosDeCampo = table.Column<int[]>(type: "integer[]", nullable: false),
                    RegistradoPorId = table.Column<int>(type: "integer", nullable: false),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vistorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vistorias_agendamentos_vistoria_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "agendamentos_vistoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vistorias_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vistorias_usuarios_RegistradoPorId",
                        column: x => x.RegistradoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "encaminhamentos_finais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    Encaminhamentos = table.Column<int[]>(type: "integer[]", nullable: false),
                    RetornoEncaminhamentos = table.Column<string>(type: "text", nullable: true),
                    RelatorioVistoriaId = table.Column<int>(type: "integer", nullable: true),
                    EntregaRelatorio = table.Column<string>(type: "text", nullable: false),
                    RegistradoPorId = table.Column<int>(type: "integer", nullable: false),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encaminhamentos_finais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_encaminhamentos_finais_arquivos_RelatorioVistoriaId",
                        column: x => x.RelatorioVistoriaId,
                        principalTable: "arquivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_encaminhamentos_finais_ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_encaminhamentos_finais_usuarios_RegistradoPorId",
                        column: x => x.RegistradoPorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        principalColumn: "id");
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
                name: "IX_agendamentos_vistoria_AgendadoPorId",
                table: "agendamentos_vistoria",
                column: "AgendadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_vistoria_OcorrenciaId_Numero",
                table: "agendamentos_vistoria",
                columns: new[] { "OcorrenciaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_vistoria_Vistoriador1Id",
                table: "agendamentos_vistoria",
                column: "Vistoriador1Id");

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_vistoria_Vistoriador2Id",
                table: "agendamentos_vistoria",
                column: "Vistoriador2Id");

            migrationBuilder.CreateIndex(
                name: "idx_arquivos_caminho",
                table: "arquivos",
                column: "caminho_relativo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_enviado_por",
                table: "arquivos",
                column: "enviado_por");

            migrationBuilder.CreateIndex(
                name: "IX_arquivos_ocorrencia_id",
                table: "arquivos",
                column: "ocorrencia_id");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_risco_AbertaPorUsuarioId",
                table: "avaliacoes_risco",
                column: "AbertaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_risco_Emergencia",
                table: "avaliacoes_risco",
                column: "Emergencia");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_risco_GrauRiscoInicial",
                table: "avaliacoes_risco",
                column: "GrauRiscoInicial");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_risco_OcorrenciaId",
                table: "avaliacoes_risco",
                column: "OcorrenciaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_encaminhamentos_finais_OcorrenciaId",
                table: "encaminhamentos_finais",
                column: "OcorrenciaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_encaminhamentos_finais_RegistradoPorId",
                table: "encaminhamentos_finais",
                column: "RegistradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_encaminhamentos_finais_RelatorioVistoriaId",
                table: "encaminhamentos_finais",
                column: "RelatorioVistoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_localizacoes_Bairro",
                table: "localizacoes",
                column: "Bairro");

            migrationBuilder.CreateIndex(
                name: "IX_localizacoes_Cep",
                table: "localizacoes",
                column: "Cep");

            migrationBuilder.CreateIndex(
                name: "IX_localizacoes_OcorrenciaId",
                table: "localizacoes",
                column: "OcorrenciaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_log_acesso_lgpd_ArquivoId",
                table: "log_acesso_lgpd",
                column: "ArquivoId");

            migrationBuilder.CreateIndex(
                name: "IX_log_acesso_lgpd_OcorrenciaId",
                table: "log_acesso_lgpd",
                column: "OcorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_log_acesso_lgpd_RegistradoEm",
                table: "log_acesso_lgpd",
                column: "RegistradoEm");

            migrationBuilder.CreateIndex(
                name: "IX_log_acesso_lgpd_UsuarioId",
                table: "log_acesso_lgpd",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_notificados_OcorrenciaId",
                table: "notificados",
                column: "OcorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_notificados_RegistradoPorId",
                table: "notificados",
                column: "RegistradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Observacoes_OcorrenciaId",
                table: "Observacoes",
                column: "OcorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Observacoes_UsuarioId",
                table: "Observacoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_AbertaEm",
                table: "ocorrencias",
                column: "AbertaEm");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_CriadoPorId",
                table: "ocorrencias",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_DeletedAt",
                table: "ocorrencias",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_ExcluidoPorId",
                table: "ocorrencias",
                column: "ExcluidoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_Protocolo",
                table: "ocorrencias",
                column: "Protocolo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_SolicitanteId",
                table: "ocorrencias",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_Status",
                table: "ocorrencias",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tentativas_vistoria_AgendamentoId_NumeroTentativa",
                table: "tentativas_vistoria",
                columns: new[] { "AgendamentoId", "NumeroTentativa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Cpf",
                table: "usuarios",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_AgendamentoId",
                table: "vistorias",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_DataVistoria",
                table: "vistorias",
                column: "DataVistoria");

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_GrauRiscoEncontrado",
                table: "vistorias",
                column: "GrauRiscoEncontrado");

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_OcorrenciaId_Numero",
                table: "vistorias",
                columns: new[] { "OcorrenciaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_RegistradoPorId",
                table: "vistorias",
                column: "RegistradoPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avaliacoes_risco");

            migrationBuilder.DropTable(
                name: "encaminhamentos_finais");

            migrationBuilder.DropTable(
                name: "localizacoes");

            migrationBuilder.DropTable(
                name: "log_acesso_lgpd");

            migrationBuilder.DropTable(
                name: "notificados");

            migrationBuilder.DropTable(
                name: "Observacoes");

            migrationBuilder.DropTable(
                name: "tentativas_vistoria");

            migrationBuilder.DropTable(
                name: "vistorias");

            migrationBuilder.DropTable(
                name: "arquivos");

            migrationBuilder.DropTable(
                name: "agendamentos_vistoria");

            migrationBuilder.DropTable(
                name: "ocorrencias");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropSequence(
                name: "seq_protocolo_ano");
        }
    }
}
