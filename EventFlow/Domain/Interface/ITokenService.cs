using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface ITokenService
    {
        string GerarToken(Usuario usuario);
        string GerarRefreshToken();
        string HashRefreshToken(string refreshToken);
    }
}
