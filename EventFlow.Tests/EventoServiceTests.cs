using EventFlow.Application.DTOs.Request;
using EventFlow.Application.Services;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Enums;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace EventFlow.Tests;

public class EventoServiceTests
{
    private static EventFlowDbContext CriarContextoEmMemoria()
    {
        var conexao = new SqliteConnection("Filename=:memory:");
        conexao.Open();

        var options = new DbContextOptionsBuilder<EventFlowDbContext>()
            .UseSqlite(conexao)
            .Options;

        var context = new EventFlowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CriarEventoAsync_DeveLancarArgumentException_QuandoDataNoPassado()
    {
        // Arrange
        await using var context = CriarContextoEmMemoria();
        var repositorioMock = new Mock<IEventoRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new EventoService(repositorioMock.Object, context, cache);

        var request = new CriarEventoRequest
        {
            Titulo = "Show de teste",
            Descricao = "Descricao",
            DataHora = DateTime.UtcNow.AddDays(-1), // no passado
            Local = "Local X",
            CapacidadeMaxima = 100,
            PrecoIngresso = 50
        };

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CriarEventoAsync(request, organizadorId: 1));
    }

    [Fact]
    public async Task AtualizarEventoAsync_DeveLancarInvalidOperationException_QuandoCapacidadeMenorQueVendidos()
    {
        // Arrange
        await using var context = CriarContextoEmMemoria();

        // O Evento tem uma foreign key obrigatoria pra Usuario (OrganizadorId).
        // Sem um Usuario de verdade salvo antes, o SaveChangesAsync abaixo
        // quebra com "FOREIGN KEY constraint failed" (DbUpdateException).
        context.Usuarios.Add(new Usuario
        {
            Id = 1,
            Nome = "Ana Organizadora",
            Email = "ana@teste.com",
            Perfil = PerfilUsuario.Organizador
        });

        context.Eventos.Add(new Evento
        {
            Id = 1,
            Titulo = "Rock in Rio",
            OrganizadorId = 1,
            DataHora = DateTime.UtcNow.AddMonths(1),
            CapacidadeMaxima = 100,
            IngressosVendidos = 80,
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new EventoService(
            new Mock<IEventoRepository>().Object, context, new MemoryCache(new MemoryCacheOptions()));

        var request = new AtualizarEventoRequest { CapacidadeMaxima = 50 /* menor que os 80 ja vendidos */ };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AtualizarEventoAsync(1, request, organizadorId: 1));
    }
}
