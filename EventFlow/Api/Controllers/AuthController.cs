using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventFlow.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const int MinutosAcesso = 15;
        private const int DiasSessao = 7;

        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var usuario = await _authService.AutenticarAsync(request.Email, request.Senha);
            if (usuario is null)
                return Unauthorized(new { mensagem = "Email ou senha inválidos." });

            await AbrirSessaoAsync(usuario);
            return Ok(new { mensagem = "Login efetuado" });
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar(RegistrarRequest request)
        {
            var sucesso = await _authService.RegistrarAsync(request);
            if (!sucesso)
                return Conflict(new { mensagem = "Email já cadastrado" });

            return Ok(new { mensagem = "Usuário registrado com sucesso." });
        }

        // Sem [Authorize] de proposito: e chamado justamente quando o cookie
        // de acesso ja expirou. Quem autoriza aqui e o cookie de sessao.
        [HttpPost("renovar")]
        public async Task<IActionResult> Renovar()
        {
            if (!Request.Cookies.TryGetValue("sessao", out var tokenSessao))
                return Unauthorized(new { mensagem = "Sessão expirada." });

            var usuario = await _authService.ValidarSessaoAsync(
                _tokenService.HashTokenSessao(tokenSessao));

            if (usuario is null)
                return Unauthorized(new { mensagem = "Sessão expirada." });

            await AbrirSessaoAsync(usuario);
            return Ok(new { mensagem = "Sessão renovada" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("sessao", out var tokenSessao))
                await _authService.EncerrarAsync(_tokenService.HashTokenSessao(tokenSessao));

            Response.Cookies.Delete("acesso", OpcoesCookie(null));
            Response.Cookies.Delete("sessao", OpcoesCookie(null));

            return Ok(new { mensagem = "Logout efetuado" });
        }

        // Emite o par de cookies e registra a sessao no banco. E essa linha
        // no banco que torna possivel encerrar a sessao antes da hora.
        private async Task AbrirSessaoAsync(Domain.Entity.Usuario usuario)
        {
            var acesso = _tokenService.GerarToken(usuario);
            var tokenSessao = _tokenService.GerarTokenSessao();

            await _authService.CriarSessaoAsync(
                usuario.Id, _tokenService.HashTokenSessao(tokenSessao));

            Response.Cookies.Append("acesso", acesso,
                OpcoesCookie(DateTimeOffset.UtcNow.AddMinutes(MinutosAcesso)));

            Response.Cookies.Append("sessao", tokenSessao,
                OpcoesCookie(DateTimeOffset.UtcNow.AddDays(DiasSessao)));
        }

        // HttpOnly: o JavaScript nao le  -> barra XSS
        // Secure: so trafega em HTTPS
        // SameSite=Strict: outro site nao dispara requisicao autenticada -> freia CSRF
        private static CookieOptions OpcoesCookie(DateTimeOffset? expira) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expira
        };
    }
}
