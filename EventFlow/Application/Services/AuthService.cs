using EventFlow.Domain.Interface;
using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Application.Services
{
    public class AuthService : IAuthService
    {
        private const int DiasValidadeRefresh = 7;

        private readonly EventFlowDbContext _db;

        public AuthService(EventFlowDbContext db)
        {
            _db = db;
        }

        public async Task<Usuario?> AutenticarAsync(string email, string senha)
        {
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (usuario == null) return null;

            bool senhaValida = BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash);
            return senhaValida ? usuario : null;
        }
        
        public async Task<bool> RegistrarAsync(RegistrarRequest request)
        {
            var usuario = new Usuario
            {
                Nome = request.Nome,
                Email = request.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                Perfil = request.Perfil
            };


            try
            {
                _db.Usuarios.Add(usuario);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }

        }

        public async Task<RefreshToken> CriarRefreshTokenAsync(int usuarioId, string tokenHash)
        {
            var refresh = new RefreshToken
            {
                UsuarioId = usuarioId,
                TokenHash = tokenHash,
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.AddDays(DiasValidadeRefresh)
            };

            _db.RefreshTokens.Add(refresh);
            await _db.SaveChangesAsync();
            return refresh;
        }

        public async Task<Usuario?> ValidarRefreshTokenAsync(string tokenHash)
        {
            var refresh = await _db.RefreshTokens
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

            if (refresh is null) return null;

            // Um token ja revogado sendo reapresentado significa que existe
            // uma copia em circulacao. Na duvida, derruba tudo do usuario.
            if (refresh.RevogadoEm is not null)
            {
                await RevogarTodosDoUsuarioAsync(refresh.UsuarioId);
                return null;
            }

            if (!refresh.Ativo) return null;

            // Rotacao: cada uso queima o token.
            refresh.RevogadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return refresh.Usuario;
        }

        public async Task RevogarAsync(string tokenHash)
        {
            var refresh = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && r.RevogadoEm == null);

            if (refresh is null) return;

            refresh.RevogadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task RevogarTodosDoUsuarioAsync(int usuarioId)
        {
            await _db.RefreshTokens
                .Where(r => r.UsuarioId == usuarioId && r.RevogadoEm == null)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevogadoEm, DateTime.UtcNow));
        }
    }
}
