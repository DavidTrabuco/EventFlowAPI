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
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.TokenHash).IsRequired().HasMaxLength(88);
                entity.HasIndex(r => r.TokenHash).IsUnique();

                entity.HasOne(r => r.Usuario)
                      .WithMany()
                      .HasForeignKey(r => r.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => r.UsuarioId);
            });
        }
    }
}
