using EventFlow.Domain.Interface;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Application.Services
{
    public class AuthService : IAuthService
    {
        private const int DiasValidadeSessao = 7;

        private readonly IUsuarioRepository _usuarios;  
        private readonly EventFlowDbContext _db;         

        public AuthService(IUsuarioRepository usuarios, EventFlowDbContext db)
        {
            _usuarios = usuarios;
            _db = db;
        }

        public async Task<Usuario?> AutenticarAsync(string email, string senha)
        {
            var usuario = await _usuarios.ObterPorEmailAsync(email);
            if (usuario == null) return null;

            // Conta que so existe via Google nao tem SenhaHash: nao ha
            // como autenticar por senha local, entao nega direto.
            if (usuario.SenhaHash is null) return null;

            bool senhaValida = BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash);
            return senhaValida ? usuario : null;
        }

        public async Task<bool> RegistrarAsync(RegistrarRequest request)
        {
            // A consulta previa da a resposta rapida...
            if (await _usuarios.EmailJaExisteAsync(request.Email))
                return false;

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
                // ...e o indice unico e a rede de seguranca, para quando duas
                // requisicoes com o mesmo email passarem juntas pela consulta.
                return false;
            }

        }

        public async Task<Usuario> ObterOuCriarViaGoogleAsync(string googleId, string email, string nome)
        {
            var usuarioPorGoogleId = await _usuarios.ObterPorGoogleIdAsync(googleId);
            if (usuarioPorGoogleId is not null)
                return usuarioPorGoogleId;

            // Ja existe conta local com esse email (cadastrada com senha)?
            // Vincula a conta Google a ela em vez de criar duplicada.
            var usuarioPorEmail = await _usuarios.ObterPorEmailAsync(email);
            if (usuarioPorEmail is not null)
            {
                var usuarioParaVincular = await _db.Usuarios.FirstAsync(u => u.Id == usuarioPorEmail.Id);
                usuarioParaVincular.GoogleId = googleId;
                await _db.SaveChangesAsync();
                return usuarioParaVincular;
            }

            var novoUsuario = new Usuario
            {
                Nome = nome,
                Email = email,
                GoogleId = googleId,
                SenhaHash = null
            };

            _db.Usuarios.Add(novoUsuario);
            await _db.SaveChangesAsync();
            return novoUsuario;
        }

        public async Task<Sessao> CriarSessaoAsync(int usuarioId, string tokenHash)
        {
            var sessao = new Sessao
            {
                UsuarioId = usuarioId,
                TokenHash = tokenHash,
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.AddDays(DiasValidadeSessao)
            };

            _db.Sessoes.Add(sessao);
            await _db.SaveChangesAsync();
            return sessao;
        }

        public async Task<Usuario?> ValidarSessaoAsync(string tokenHash)
        {
            var sessao = await _db.Sessoes
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            if (sessao is null) return null;

            // Um token ja encerrado sendo reapresentado significa que existe
            // uma copia em circulacao. Na duvida, derruba tudo do usuario.
            if (sessao.EncerradaEm is not null)
            {
                await EncerrarTodasDoUsuarioAsync(sessao.UsuarioId);
                return null;
            }

            if (!sessao.Ativa) return null;

            // Rotacao: cada uso queima o token da sessao.
            sessao.EncerradaEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return sessao.Usuario;
        }

        public async Task EncerrarAsync(string tokenHash)
        {
            var sessao = await _db.Sessoes
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.EncerradaEm == null);

            if (sessao is null) return;

            sessao.EncerradaEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task EncerrarTodasDoUsuarioAsync(int usuarioId)
        {
            await _db.Sessoes
                .Where(x => x.UsuarioId == usuarioId && x.EncerradaEm == null)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.EncerradaEm, DateTime.UtcNow));
        }
    }
}
