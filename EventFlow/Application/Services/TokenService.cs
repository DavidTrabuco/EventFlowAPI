

using EventFlow.Domain.Interface;
using EventFlow.Domain.Entity;
using Microsoft.IdentityModel.Tokens;
using EventFlow.Domain.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace EventFlow.Application.Services
{
    public class TokenService : ITokenService
    {

        private readonly JwtOptions _jwtOptions;

        public TokenService(IConfiguration configuration, IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public string GerarToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(
                _jwtOptions.Key ?? throw new InvalidOperationException("JWT Key is not configured."));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Perfil.ToString())
        ]),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // 32 bytes de entropia criptografica. Nunca Random: ele e previsivel.
        public string GerarTokenSessao()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }

        // SHA-256 e nao BCrypt: o valor ja tem 256 bits de entropia,
        // forca bruta e inviavel e a lentidao do BCrypt so atrasaria a renovacao.
        public string HashTokenSessao(string tokenSessao)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenSessao));
            return Convert.ToBase64String(bytes);
        }

    }
}
