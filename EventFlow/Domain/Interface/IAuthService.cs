using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IAuthService
    {

        Task<string?> AutenticarAsync(string email, string senha);
        Task RegistrarAsync(RegistrarRequest request);
    }
}
