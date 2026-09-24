using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Enums;
using EventFlow.Infrastructure.Data;
using EventFlow.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace EventFlow.Tests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly PostgresTestSchema _schema;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _schema = PostgresTestSchema.Criar();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Mesma troca do EventoControllerTests: Postgres do appsettings ->
                // schema isolado, por execucao. O Program.cs roda igual, incluindo o DbSeeder.
                services.RemoveAll<DbContextOptions<EventFlowDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<EventFlowDbContext>>();

                services.AddDbContext<EventFlowDbContext>(options =>
                    options.UseNpgsql(_schema.Conexao).UseSnakeCaseNamingConvention());
            });
        }).CreateClient();
    }

    public void Dispose() => _schema.Derrubar();

    [Fact]
    public async Task Login_DeveRetornar401_QuandoCredenciaisInvalidas()
    {
        var request = new LoginRequest { Email = "naoexiste@teste.com", Senha = "qualquer" };

        var resposta = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_DeveDefinirCookiesDeAcessoESessao_QuandoCredenciaisValidas()
    {
        // Nao precisa registrar ninguem: o DbSeeder ja cria a Ana (ana@teste.com)
        // com a senha padrao assim que o host sobe, igual acontece na sua maquina.
        var request = new LoginRequest { Email = "ana@teste.com", Senha = DbSeeder.SenhaPadrao };

        var resposta = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        // AbrirSessaoAsync (no AuthController) seta os dois cookies no sucesso.
        Assert.True(resposta.Headers.TryGetValues("Set-Cookie", out var cookies));
        var listaCookies = cookies!.ToList();
        Assert.Contains(listaCookies, c => c.StartsWith("acesso="));
        Assert.Contains(listaCookies, c => c.StartsWith("sessao="));
    }

    [Fact]
    public async Task Registrar_DeveRetornar409_QuandoEmailJaExiste()
    {
        // ana@teste.com ja existe por causa do DbSeeder.
        var request = new RegistrarRequest
        {
            Nome = "Outra Ana",
            Email = "ana@teste.com",
            Senha = "outraSenha123",
            Perfil = PerfilUsuario.Participante
        };

        var resposta = await _client.PostAsJsonAsync("/api/auth/registrar", request);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Registrar_DeveRetornar200_QuandoEmailNovo()
    {
        var request = new RegistrarRequest
        {
            Nome = "Usuario Novo",
            Email = "novo.integracao@teste.com",
            Senha = "senha123",
            Perfil = PerfilUsuario.Participante
        };

        var resposta = await _client.PostAsJsonAsync("/api/auth/registrar", request);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
