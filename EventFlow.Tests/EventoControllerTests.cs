using EventFlow.Infrastructure.Data;
using EventFlow.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace EventFlow.Tests;

public class EventoControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly PostgresTestSchema _schema;
    private readonly HttpClient _client;

    public EventoControllerTests(WebApplicationFactory<Program> factory)
    {
        _schema = PostgresTestSchema.Criar();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Troca o Postgres do appsettings por um schema isolado, so
                // pra esta execucao de testes (ver PostgresTestSchema).
                services.RemoveAll<DbContextOptions<EventFlowDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<EventFlowDbContext>>();

                services.AddDbContext<EventFlowDbContext>(options =>
                    options.UseNpgsql(_schema.Conexao).UseSnakeCaseNamingConvention());
            });
        }).CreateClient();
    }

    public void Dispose() => _schema.Derrubar();

    [Fact]
    public async Task ListarEventos_SemAutenticacao_DeveRetornar401()
    {
        var resposta = await _client.GetAsync("/api/evento");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
