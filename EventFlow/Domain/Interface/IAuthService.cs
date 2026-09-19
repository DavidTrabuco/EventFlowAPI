using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IAuthService
    {
        Task<Usuario?> AutenticarAsync(string email, string senha);
        Task<bool> RegistrarAsync(RegistrarRequest request);

        Task<bool> DeletarUsuarioAsync(int id);

        // Busca o usuario pelo GoogleId; se nao existir, cria um novo
        // (sem senha local) com os dados vindos do Google.
        Task<Usuario> ObterOuCriarViaGoogleAsync(string googleId, string email, string nome);

        Task<Sessao> CriarSessaoAsync(int usuarioId, string tokenHash);
        Task<Usuario?> ValidarSessaoAsync(string tokenHash);
        Task EncerrarAsync(string tokenHash);
        Task EncerrarTodasDoUsuarioAsync(int usuarioId);

    }
}
