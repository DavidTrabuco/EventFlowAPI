using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface ITokenServices
    {
        string GerarToken(Usuario usuario);
    }
}
