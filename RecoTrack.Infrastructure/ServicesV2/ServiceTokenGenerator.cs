using Microsoft.IdentityModel.Tokens;
using RecoTrack.Application.Interfaces;
using RecoTrack.Shared.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace RecoTrack.Infrastructure.ServicesV2
{
    public class ServiceTokenGenerator : IServiceTokenGenerator
    {
        private readonly ServiceJwtSettings _settings;

        public ServiceTokenGenerator(ServiceJwtSettings settings)
        {
            _settings = settings;
        }

        public string GenerateToken()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, _settings.Issuer),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique ID to prevent replay
            new Claim("scope", "access:EmailService")
        };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
