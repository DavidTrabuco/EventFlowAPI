using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface ITokenService
    {
        string GerarToken(Usuario usuario);
        string GerarTokenSessao();
        string HashTokenSessao(string tokenSessao);
    }
}
