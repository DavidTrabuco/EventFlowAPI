using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const int MinutosAccessToken = 15;
        private const int DiasRefreshToken = 7;
        private const string CaminhoRefresh = "/api/auth";

        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var usuario = await _authService.AutenticarAsync(request.Email, request.Senha);
            if (usuario is null)
                return Unauthorized(new { mensagem = "Email ou senha inválidos." });

            await EmitirSessaoAsync(usuario.Id, _tokenService.GerarToken(usuario));
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

        // Sem [Authorize] de proposito: e chamado justamente quando o
        // access token ja expirou. Quem autoriza aqui e o refresh token.
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
                return Unauthorized(new { mensagem = "Sessão expirada." });

            var usuario = await _authService.ValidarRefreshTokenAsync(
                _tokenService.HashRefreshToken(refreshToken));

            if (usuario is null)
                return Unauthorized(new { mensagem = "Sessão expirada." });

            await EmitirSessaoAsync(usuario.Id, _tokenService.GerarToken(usuario));
            return Ok(new { mensagem = "Sessão renovada" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
                await _authService.RevogarAsync(_tokenService.HashRefreshToken(refreshToken));

            Response.Cookies.Delete("access_token", OpcoesAccess(null));
            Response.Cookies.Delete("refresh_token", OpcoesRefresh(null));

            return Ok(new { mensagem = "Logout efetuado" });
        }

        // Emite o par e grava os dois cookies. Cada login/refresh cria
        // uma linha nova no banco: e ela que torna a revogacao possivel.
        private async Task EmitirSessaoAsync(int usuarioId, string accessToken)
        {
            var refreshToken = _tokenService.GerarRefreshToken();

            await _authService.CriarRefreshTokenAsync(
                usuarioId, _tokenService.HashRefreshToken(refreshToken));

            Response.Cookies.Append("access_token", accessToken,
                OpcoesAccess(DateTimeOffset.UtcNow.AddMinutes(MinutosAccessToken)));

            Response.Cookies.Append("refresh_token", refreshToken,
                OpcoesRefresh(DateTimeOffset.UtcNow.AddDays(DiasRefreshToken)));
        }

        private static CookieOptions OpcoesAccess(DateTimeOffset? expira) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expira
        };

        // Path restrito: o refresh token nao trafega nas demais rotas da API.
        private static CookieOptions OpcoesRefresh(DateTimeOffset? expira) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CaminhoRefresh,
            Expires = expira
        };
    }
}
