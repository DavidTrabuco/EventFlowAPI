using EventFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace EventFlow.Tests;

public class EventoControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EventoControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Troca o SQLite de arquivo (EventFlow.db) por um banco em
                // memoria isolado, so pra esta execucao de testes.
                services.RemoveAll<DbContextOptions<EventFlowDbContext>>();

                var conexao = new SqliteConnection("Filename=:memory:");
                conexao.Open();
                services.AddDbContext<EventFlowDbContext>(options => options.UseSqlite(conexao));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task ListarEventos_SemAutenticacao_DeveRetornar401()
    {
        var resposta = await _client.GetAsync("/api/evento");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
