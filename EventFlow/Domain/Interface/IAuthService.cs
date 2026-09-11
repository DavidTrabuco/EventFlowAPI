using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IAuthService
    {
        Task<Usuario?> AutenticarAsync(string email, string senha);
        Task<bool> RegistrarAsync(RegistrarRequest request);

        Task<Sessao> CriarSessaoAsync(int usuarioId, string tokenHash);
        Task<Usuario?> ValidarSessaoAsync(string tokenHash);
        Task EncerrarAsync(string tokenHash);
        Task EncerrarTodasDoUsuarioAsync(int usuarioId);
    }
}
