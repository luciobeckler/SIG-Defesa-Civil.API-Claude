namespace SIG_Defesa_Civil.API.Data.Models
{
    using Microsoft.EntityFrameworkCore;
    using SIG_Defesa_Civil.API.Data.Entities.Tabelas.Ocorrencia;
    using SIG_Defesa_Civil.API.Data.Models.Tabelas;
    using SIG_Defesa_Civil.API.Enums;

    public class DefesaCivilContext : DbContext
    {
        public DefesaCivilContext(DbContextOptions<DefesaCivilContext> options) : base(options) { }

        // ── DbSets ────────────────────────────────────────────────────────────────
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Ocorrencia> Ocorrencias { get; set; }
        public DbSet<Localizacao> Localizacoes { get; set; }
        public DbSet<Arquivo> Arquivos { get; set; }
        public DbSet<Observacao> Observacoes { get; set; }
        public DbSet<LogAcessoLgpd> LogsLgpd { get; set; }

        // Etapas 2–6
        public DbSet<AvaliacaoRisco> AvaliacoesRisco { get; set; }
        public DbSet<AgendamentoVistoria> AgendamentosVistoria { get; set; }
        public DbSet<TentativaVistoria> TentativasVistoria { get; set; }
        public DbSet<Vistoria> Vistorias { get; set; }
        public DbSet<Notificado> Notificados { get; set; }
        public DbSet<EncaminhamentoFinal> EncaminhamentosFinais { get; set; }
        public DbSet<OpcaoCampoVistoria> OpcoesCampoVistoria { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── Sequence de protocolo ─────────────────────────────────────────────
            modelBuilder.HasSequence<int>("seq_protocolo_ano")
                .StartsAt(1)
                .IncrementsBy(1);

            // ═══════════════════════════════════════════════════════════════════════
            // USUARIO
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.Property(u => u.TipoUsuario).HasConversion<string>();
                entity.HasIndex(u => u.Cpf).IsUnique();
                entity.HasIndex(u => u.Email);
            });

            // ═══════════════════════════════════════════════════════════════════════
            // OCORRENCIA
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Ocorrencia>(entity =>
            {
                entity.Property(o => o.Status).HasConversion<string>();

                // Índices de performance para listagem/filtro
                entity.HasIndex(o => o.Protocolo).IsUnique();
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.AbertaEm);
                entity.HasIndex(o => o.DeletedAt); // filtrar soft-deleted eficientemente

                // Solicitante: owned — colunas na própria tabela, sem FK para usuarios.
                // Cidadãos não têm conta no sistema (abertura é endpoint público).
                entity.OwnsOne(o => o.Solicitante, sol =>
                {
                    sol.Property(s => s.Nome).HasColumnName("SolicitanteNome")
                       .HasMaxLength(200).IsRequired();
                    sol.Property(s => s.Cpf).HasColumnName("SolicitanteCpf").HasMaxLength(11);
                    sol.Property(s => s.Rg).HasColumnName("SolicitanteRg").HasMaxLength(20);
                    sol.Property(s => s.OrgaoEmissor).HasColumnName("SolicitanteOrgaoEmissor").HasMaxLength(20);
                    sol.Property(s => s.Email).HasColumnName("SolicitanteEmail").HasMaxLength(200);
                    // 50: a planilha histórica traz mais de um número no mesmo campo
                    sol.Property(s => s.Telefone).HasColumnName("SolicitanteTelefone").HasMaxLength(50);
                    sol.Property(s => s.Celular).HasColumnName("SolicitanteCelular").HasMaxLength(50);

                    // Filtro "início do CPF" na listagem
                    sol.HasIndex(s => s.Cpf);
                });
                entity.Navigation(o => o.Solicitante).IsRequired();

                // FK: Quem criou o registro no sistema (nulo = aberta pelo portal público)
                entity.HasOne(o => o.CriadoPor)
                    .WithMany()
                    .HasForeignKey(o => o.CriadoPorId)
                    .OnDelete(DeleteBehavior.SetNull);

                // FK: Quem realizou o soft-delete (nullable)
                entity.HasOne(o => o.ExcluidoPor)
                    .WithMany()
                    .HasForeignKey(o => o.ExcluidoPorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ═══════════════════════════════════════════════════════════════════════
            // LOCALIZACAO (Etapa 1 — 1:1 dependente)
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Localizacao>(entity =>
            {
                entity.HasOne(l => l.Ocorrencia)
                    .WithOne(o => o.Localizacao)
                    .HasForeignKey<Localizacao>(l => l.OcorrenciaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(l => l.Bairro);
                entity.HasIndex(l => l.Cep);
            });

            // ═══════════════════════════════════════════════════════════════════════
            // ARQUIVO
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Arquivo>(entity =>
            {
                entity.ToTable("arquivos");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.OcorrenciaId).HasColumnName("ocorrencia_id");
                entity.Property(e => e.NomeOriginal).HasColumnName("nome_original").HasMaxLength(255).IsRequired();
                entity.Property(e => e.TipoArquivo).HasColumnName("tipo_arquivo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.CaminhoRelativo).HasColumnName("caminho_relativo").IsRequired();
                entity.Property(e => e.TamanhoBytes).HasColumnName("tamanho_bytes").IsRequired();
                entity.Property(e => e.EnviadoPorUserId).HasColumnName("enviado_por");
                entity.Property(e => e.EnviadoEm).HasColumnName("enviado_em");

                entity.HasIndex(e => e.CaminhoRelativo).HasDatabaseName("idx_arquivos_caminho").IsUnique();

                entity.HasOne(a => a.Ocorrencia)
                    .WithMany(o => o.Arquivos)
                    .HasForeignKey(a => a.OcorrenciaId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Nulo nos anexos enviados pelo cidadão na abertura pública
                entity.HasOne(a => a.Usuario)
                    .WithMany()
                    .HasForeignKey(a => a.EnviadoPorUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ═══════════════════════════════════════════════════════════════════════
            // LOG LGPD
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<LogAcessoLgpd>(entity =>
            {
                entity.Property(l => l.Acao).HasConversion<string>();

                // Nulo quando a ação partiu do portal público (sem usuário autenticado)
                entity.HasOne(l => l.Usuario)
                    .WithMany()
                    .HasForeignKey(l => l.UsuarioId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(l => l.UsuarioId);
                entity.HasIndex(l => l.OcorrenciaId);
                entity.HasIndex(l => l.RegistradoEm);
            });

            // ═══════════════════════════════════════════════════════════════════════
            // AVALIACAO DE RISCO (Etapa 2 — 1:1 dependente)
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<AvaliacaoRisco>(entity =>
            {
                // TipificacaoInicial é text[] (multi-seleção) — sem conversão de enum
                entity.Property(a => a.GrauRiscoInicial).HasConversion<string>();

                entity.HasOne(a => a.Ocorrencia)
                    .WithOne(o => o.AvaliacaoRisco)
                    .HasForeignKey<AvaliacaoRisco>(a => a.OcorrenciaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.AbertaPorUsuario)
                    .WithMany()
                    .HasForeignKey(a => a.AbertaPorUsuarioId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(a => a.GrauRiscoInicial);
                entity.HasIndex(a => a.Emergencia);
            });

            // ═══════════════════════════════════════════════════════════════════════
            // AGENDAMENTO DE VISTORIA (Etapa 3 — 1:N dependente)
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<AgendamentoVistoria>(entity =>
            {
                entity.Property(a => a.Status).HasConversion<string>();

                entity.HasOne(a => a.Ocorrencia)
                    .WithMany(o => o.Agendamentos)
                    .HasForeignKey(a => a.OcorrenciaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Vistoriador1)
                    .WithMany()
                    .HasForeignKey(a => a.Vistoriador1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Vistoriador2)
                    .WithMany()
                    .HasForeignKey(a => a.Vistoriador2Id)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Vistoriador3)
                    .WithMany()
                    .HasForeignKey(a => a.Vistoriador3Id)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Vistoriador4)
                    .WithMany()
                    .HasForeignKey(a => a.Vistoriador4Id)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.AgendadoPor)
                    .WithMany()
                    .HasForeignKey(a => a.AgendadoPorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Garante que o número do agendamento é único por ocorrência
                entity.HasIndex(a => new { a.OcorrenciaId, a.Numero }).IsUnique();
            });

            // ═══════════════════════════════════════════════════════════════════════
            // TENTATIVA DE VISTORIA (Etapa 3 — 1:N do AgendamentoVistoria)
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<TentativaVistoria>(entity =>
            {
                entity.HasOne(t => t.Agendamento)
                    .WithMany(a => a.Tentativas)
                    .HasForeignKey(t => t.AgendamentoId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Garante que o número da tentativa é único por agendamento
                entity.HasIndex(t => new { t.AgendamentoId, t.NumeroTentativa }).IsUnique();
            });

            // ═══════════════════════════════════════════════════════════════════════
            // VISTORIA PRESENCIAL (Etapa 4 — 1:N dependente)
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Vistoria>(entity =>
            {
                // Campos de classificação são texto (single → text, multi → text[]),
                // permitindo opções personalizadas do catálogo além dos enums fixos.

                entity.HasOne(v => v.Ocorrencia)
                    .WithMany(o => o.Vistorias)
                    .HasForeignKey(v => v.OcorrenciaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.Agendamento)
                    .WithMany()
                    .HasForeignKey(v => v.AgendamentoId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(v => v.RegistradoPor)
                    .WithMany()
                    .HasForeignKey(v => v.RegistradoPorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(v => v.GrauRiscoEncontrado);
                entity.HasIndex(v => v.DataVistoria);

                // Garante que o número da vistoria é único por ocorrência
                entity.HasIndex(v => new { v.OcorrenciaId, v.Numero }).IsUnique();
            });

            // ═══════════════════════════════════════════════════════════════════════
            // CATÁLOGO DE OPÇÕES PERSONALIZADAS DOS CAMPOS DE VISTORIA
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<OpcaoCampoVistoria>(entity =>
            {
                entity.Property(o => o.Campo).HasMaxLength(50);
                entity.Property(o => o.Valor).HasMaxLength(200);
                entity.Property(o => o.Label).HasMaxLength(200);

                // Não permite a mesma opção duplicada no mesmo campo
                entity.HasIndex(o => new { o.Campo, o.Valor }).IsUnique();
            });

            // ═══════════════════════════════════════════════════════════════════════
            // NOTIFICADO (Etapa 5 — 1:N)
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Notificado>(entity =>
            {
                entity.Property(n => n.FormaRecebimento).HasConversion<string>();

                entity.HasOne(n => n.Ocorrencia)
                    .WithMany(o => o.Notificados)
                    .HasForeignKey(n => n.OcorrenciaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.RegistradoPor)
                    .WithMany()
                    .HasForeignKey(n => n.RegistradoPorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════════
            // ENCAMINHAMENTO FINAL (Etapa 6 — 1:1 dependente)
            // ═══════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<EncaminhamentoFinal>(entity =>
            {

                entity.HasOne(e => e.Ocorrencia)
                    .WithOne(o => o.EncaminhamentoFinal)
                    .HasForeignKey<EncaminhamentoFinal>(e => e.OcorrenciaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RegistradoPor)
                    .WithMany()
                    .HasForeignKey(e => e.RegistradoPorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relatório é um arquivo do tipo RELATORIO_FINAL (nullable)
                entity.HasOne(e => e.RelatorioVistoria)
                    .WithMany()
                    .HasForeignKey(e => e.RelatorioVistoriaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
