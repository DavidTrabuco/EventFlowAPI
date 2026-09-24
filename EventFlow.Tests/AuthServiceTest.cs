using EventFlow.Application.Services;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Enums;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using EventFlow.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EventFlow.Tests
{
    public class AuthServiceTest
    {
        private static EventFlowDbContext CriarContextoEmMemoria() =>
            PostgresTestContextFactory.CriarContexto();

        [Fact]
        public async Task AutenticarAsync_DeveRetornarUsuario_QuandoCredenciaisEstaoCorretas()
        {
            // Arrange
            await using var context = CriarContextoEmMemoria();

            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Ana",
                Email = "ana@teste.com",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("senhaCorreta123"),
                Perfil = PerfilUsuario.Participante
            };

            
            var repositorioMock = new Mock<IUsuarioRepository>();
            repositorioMock
                .Setup(r => r.ObterPorEmailAsync("ana@teste.com"))
                .ReturnsAsync(usuario);

            var authService = new AuthService(repositorioMock.Object, context, NullLogger<AuthService>.Instance);

            // Act
            var resultado = await authService.AutenticarAsync("ana@teste.com", "senhaCorreta123");

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(usuario.Id, resultado!.Id);
        }

        [Fact]
        public async Task AutenticarAsync_DeveRetornarNull_QuandoSenhaEstaIncorreta()
        {
            await using var context = CriarContextoEmMemoria();

            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Ana",
                Email = "ana@teste.com",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("senhaCorreta123"),
                Perfil = PerfilUsuario.Participante
            };

            var repositorioMock = new Mock<IUsuarioRepository>();
            repositorioMock
                .Setup(r => r.ObterPorEmailAsync("ana@teste.com"))
                .ReturnsAsync(usuario);

            var authService = new AuthService(repositorioMock.Object, context, NullLogger<AuthService>.Instance);

            var resultado = await authService.AutenticarAsync("ana@teste.com", "senhaErrada");

            Assert.Null(resultado);
        }

        [Fact]
        public async Task AutenticarAsync_DeveRetornarNull_QuandoEmailNaoExiste()
        {
            await using var context = CriarContextoEmMemoria();

            var repositorioMock = new Mock<IUsuarioRepository>();
            repositorioMock
                .Setup(r => r.ObterPorEmailAsync("naoexiste@teste.com"))
                .ReturnsAsync((Usuario?)null);

            var authService = new AuthService(repositorioMock.Object, context, NullLogger<AuthService>.Instance);

            var resultado = await authService.AutenticarAsync("naoexiste@teste.com", "qualquersenha");

            Assert.Null(resultado);
        }
    }
}