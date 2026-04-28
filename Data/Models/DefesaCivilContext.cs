namespace SIG_Defesa_Civil.API.Data.Models
{
    using Microsoft.EntityFrameworkCore;
    using SIG_Defesa_Civil.API.Data.Models.Tabelas;

    public class DefesaCivilContext : DbContext
    {
        public DefesaCivilContext(DbContextOptions<DefesaCivilContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Ocorrencia> Ocorrencias { get; set; }
        public DbSet<Arquivo> Arquivos { get; set; }
        public DbSet<LogAcessoLgpd> LogsLgpd { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configura a Sequence no Postgres
            modelBuilder.HasSequence<int>("seq_protocolo_ano")
                .StartsAt(1)
                .IncrementsBy(1);

            // Conversão de Enums para String no banco de dados para facilitar leitura externa
            modelBuilder.Entity<Usuario>()
                .Property(u => u.TipoUsuario)
                .HasConversion<string>();

            modelBuilder.Entity<Ocorrencia>()
                .Property(o => o.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Arquivo>()
                .Property(a => a.TipoArquivo)
                .HasConversion<string>();

            modelBuilder.Entity<LogAcessoLgpd>()
                .Property(l => l.Acao)
                .HasConversion<string>();

            // Índices e Unique Constraints
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Cpf).IsUnique();
            modelBuilder.Entity<Ocorrencia>().HasIndex(o => o.Protocolo).IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
