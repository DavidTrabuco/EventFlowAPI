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
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUsuarioRepository usuarios,
            EventFlowDbContext db,
            ILogger<AuthService> logger)
        {
            _usuarios = usuarios;
            _db = db;
            _logger = logger;
        }

        public async Task<Usuario?> AutenticarAsync(string email, string senha)
        {
            var usuario = await _usuarios.ObterPorEmailAsync(email);
            if (usuario == null)
            {
                _logger.LogWarning("Tentativa de login com e-mail não cadastrado: {Email}", email);
                return null;
            }

            if (usuario.SenhaHash is null)
            {
                _logger.LogWarning("Tentativa de login por senha em conta vinculada ao Google: {Email}", email);
                return null;
            }

            bool senhaValida = BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash);

            if (senhaValida)
                _logger.LogInformation("Autenticação bem-sucedida para o usuário {UserId}", usuario.Id);
            else
                _logger.LogWarning("Senha incorreta para o e-mail {Email}", email);

            return senhaValida ? usuario : null;
        }

        public async Task<bool> RegistrarAsync(RegistrarRequest request)
        {
            if (await _usuarios.EmailJaExisteAsync(request.Email))
            {
                _logger.LogInformation("Tentativa de registro com e-mail já existente: {Email}", request.Email);
                return false;
            }

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
                _logger.LogInformation("Novo usuário registrado: {UserId} ({Email})", usuario.Id, usuario.Email);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Falha ao registrar usuário com e-mail {Email} (provável corrida de concorrência)", request.Email);
                return false;
            }
        }

        public async Task<bool> DeletarUsuarioAsync(int id)
        {
            var usuario = await _usuarios.ObterUsuarioIdAsync(id);

            if (usuario == null)
            {
                _logger.LogWarning("Usuário com ID {UserId} não encontrado para deleção", id);
                return false;
            }

            // O banco apaga em cascata sessoes, participante, eventos e ingressos.
            _db.Usuarios.Remove(usuario);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Usuário com ID {UserId} deletado com sucesso", id);
            return true;
        }

        public async Task<Usuario> ObterOuCriarViaGoogleAsync(string googleId, string email, string nome)
        {
            var usuarioPorGoogleId = await _usuarios.ObterPorGoogleIdAsync(googleId);
            if (usuarioPorGoogleId is not null)
                return usuarioPorGoogleId;

            var usuarioPorEmail = await _usuarios.ObterPorEmailAsync(email);
            if (usuarioPorEmail is not null)
            {
                var usuarioParaVincular = await _db.Usuarios.FirstAsync(u => u.Id == usuarioPorEmail.Id);
                usuarioParaVincular.GoogleId = googleId;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Conta local vinculada ao Google: {UserId} ({Email})", usuarioParaVincular.Id, email);
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
            _logger.LogInformation("Novo usuário criado via Google: {UserId} ({Email})", novoUsuario.Id, email);
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
            _logger.LogInformation("Sessão criada para o usuário {UserId}", usuarioId);
            return sessao;
        }

        public async Task<Usuario?> ValidarSessaoAsync(string tokenHash)
        {
            var sessao = await _db.Sessoes
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            if (sessao is null)
            {
                _logger.LogWarning("Token de sessão não encontrado");
                return null;
            }

            if (sessao.EncerradaEm is not null)
            {
                _logger.LogWarning("Token de sessão já encerrado reapresentado: encerrando todas as sessões do usuário {UserId}", sessao.UsuarioId);
                await EncerrarTodasDoUsuarioAsync(sessao.UsuarioId);
                return null;
            }

            if (!sessao.Ativa)
            {
                _logger.LogWarning("Sessão inativa/expirada para o usuário {UserId}", sessao.UsuarioId);
                return null;
            }

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
            _logger.LogInformation("Sessão encerrada para o usuário {UserId}", sessao.UsuarioId);
        }

        public async Task EncerrarTodasDoUsuarioAsync(int usuarioId)
        {
            await _db.Sessoes
                .Where(x => x.UsuarioId == usuarioId && x.EncerradaEm == null)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.EncerradaEm, DateTime.UtcNow));

            _logger.LogInformation("Todas as sessões do usuário {UserId} foram encerradas", usuarioId);
        }
    }
}