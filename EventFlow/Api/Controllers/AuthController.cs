using System.Security.Claims;
using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
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

       
        [HttpGet("google")]
        public IActionResult LoginGoogle()
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback))
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

       
        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync("External");
            if (!result.Succeeded || result.Principal is null)
                return Unauthorized(new { mensagem = "Falha na autenticação com Google." });

            var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var nome = result.Principal.FindFirstValue(ClaimTypes.Name);

           
            await HttpContext.SignOutAsync("External");

            if (googleId is null || email is null)
                return Unauthorized(new { mensagem = "Google não retornou os dados esperados." });

            var usuario = await _authService.ObterOuCriarViaGoogleAsync(googleId, email, nome ?? email);

            await AbrirSessaoAsync(usuario);
            return Redirect("/swagger");
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

        //Junta aqui a logica de abrir sessao, gerar token de acesso e token de sessao, e setar os cookies , ISSO É MARAVILHOSO RS 
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

        
        private static CookieOptions OpcoesCookie(DateTimeOffset? expira) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expira
        };
    }
}
