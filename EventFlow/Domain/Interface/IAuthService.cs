using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IAuthService
    {
        Task<Usuario?> AutenticarAsync(string email, string senha);
        Task<bool> RegistrarAsync(RegistrarRequest request);

        Task<RefreshToken> CriarRefreshTokenAsync(int usuarioId, string tokenHash);
        Task<Usuario?> ValidarRefreshTokenAsync(string tokenHash);
        Task RevogarAsync(string tokenHash);
        Task RevogarTodosDoUsuarioAsync(int usuarioId);
    }
}
