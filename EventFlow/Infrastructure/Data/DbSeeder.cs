using EventFlow.Domain.Entity;
using EventFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Data
{
    /// <summary>
    /// Cria o banco e popula com dados de exemplo na primeira execucao.
    /// Assim quem clona o repositorio nao precisa rodar migrations nem
    /// cadastrar nada a mao — o banco nasce pronto na propria maquina.
    /// </summary>
    public static class DbSeeder
    {
        public const string SenhaPadrao = "123456";

        public static async Task InicializarAsync(EventFlowDbContext db)
        {
            // Aplica as migrations pendentes; cria o arquivo .db se nao existir.
            await db.Database.MigrateAsync();

            // Se ja houver usuario, o banco e do proprio dev: nao mexe.
            if (await db.Usuarios.AnyAsync()) return;

            var ana = new Usuario
            {
                Nome = "Ana",
                Email = "ana@teste.com",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaPadrao),
                Perfil = PerfilUsuario.Organizador
            };

            var bruno = new Usuario
            {
                Nome = "Bruno",
                Email = "bruno@teste.com",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaPadrao),
                Perfil = PerfilUsuario.Participante
            };

            db.Usuarios.AddRange(ana, bruno);
            await db.SaveChangesAsync();

            var evento = new Evento
            {
                Titulo = "Rock in Rio",
                Descricao = "Festival de musica",
                DataHora = new DateTime(2026, 12, 20, 20, 0, 0, DateTimeKind.Utc),
                Local = "Rio de Janeiro",
                CapacidadeMaxima = 50,
                IngressosVendidos = 1,
                PrecoIngresso = 120m,
                Ativo = true,
                OrganizadorId = ana.Id     // a Ana e a dona deste evento
            };

            var participante = new Participante
            {
                Nome = "Bruno",
                Email = "bruno@teste.com",
                Cpf = "12345678901",
                UsuarioId = bruno.Id
            };

            db.Eventos.Add(evento);
            db.Participantes.Add(participante);
            await db.SaveChangesAsync();

            db.Ingressos.Add(new Ingresso
            {
                EventoId = evento.Id,
                ParticipanteId = participante.Id,
                DataHoraCompra = DateTime.UtcNow,
                ValorPago = evento.PrecoIngresso,
                CodigoValidacao = "DEMO1234",
                Status = StatusIngresso.Ativo
            });

            await db.SaveChangesAsync();
        }
    }
}
