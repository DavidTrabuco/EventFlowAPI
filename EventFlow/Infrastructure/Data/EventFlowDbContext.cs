using EventFlow.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Data
{
    public class EventFlowDbContext : DbContext
    {
        public EventFlowDbContext(DbContextOptions<EventFlowDbContext> options) : base(options) { }

        public DbSet<Evento> Eventos => Set<Evento>();
        public DbSet<Participante> Participantes => Set<Participante>();
        public DbSet<Ingresso> Ingressos => Set<Ingresso>();

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Sessao> Sessoes => Set<Sessao>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Evento>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titulo).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Descricao).HasMaxLength(500);
                entity.Property(e => e.Local).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PrecoIngresso).HasPrecision(18, 2);

                // O dono do evento. Sem isso, qualquer Organizador edita o
                // evento de qualquer outro — e como o cadastro permite se
                // declarar Organizador, isso significa qualquer pessoa.
                //
                // Restrict, nao Cascade: apagar um organizador NAO pode apagar
                // os eventos dele, porque ha ingressos vendidos apontando para
                // eles. O banco recusa a exclusao ate alguem decidir o que
                // fazer com os eventos.
                //
                // Nao precisa de HasIndex: o EF cria o indice sozinho para
                // toda chave estrangeira.
                entity.HasOne<Usuario>()
                      .WithMany()
                      .HasForeignKey(e => e.OrganizadorId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Participante>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Nome).IsRequired().HasMaxLength(150);
                entity.Property(p => p.Email).IsRequired().HasMaxLength(150);
                entity.Property(p => p.Cpf).IsRequired().HasMaxLength(11);
                entity.HasIndex(p => p.Cpf).IsUnique();

                entity.HasOne(p => p.Usuario)
                      .WithMany()
                      .HasForeignKey(p => p.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(p => p.UsuarioId).IsUnique();
            });

            modelBuilder.Entity<Ingresso>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.CodigoValidacao).IsRequired().HasMaxLength(20);
                entity.Property(i => i.ValorPago).HasPrecision(18, 2);
                entity.HasIndex(i => i.CodigoValidacao).IsUnique();

                entity.HasOne(i => i.Evento)
                      .WithMany()
                      .HasForeignKey(i => i.EventoId);

                entity.HasOne(i => i.Participante)
                      .WithMany()
                      .HasForeignKey(i => i.ParticipanteId);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Nome).IsRequired().HasMaxLength(150);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
                entity.Property(u => u.SenhaHash).IsRequired();
                entity.Property(u => u.Perfil)
                      .IsRequired()
                      .HasConversion<string>()
                      .HasMaxLength(50);
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Sessao>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(88);
                entity.HasIndex(x => x.TokenHash).IsUnique();   // busca a cada renovacao

                entity.HasOne(x => x.Usuario)
                      .WithMany()
                      .HasForeignKey(x => x.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.UsuarioId);              // encerrar todas de um usuario
            });
        }
    }
}
