using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for JWT generation to decouple security infrastructure from business logic.
    /// </summary>
    public interface IJwtProvider
    {
        string GenerateToken(string userId, string roleId);
    }

    /// <summary>
    /// Implementation of the JWT generation logic.
    /// </summary>
    public class JwtProvider : IJwtProvider
    {
        private readonly IConfiguration _configuration;

        private readonly JwtSigningKey _signingKey;

        public JwtProvider(IConfiguration configuration, JwtSigningKey signingKey)
        {
            _configuration = configuration;
            _signingKey = signingKey;
        }

        public string GenerateToken(string userId, string roleId)
        {

            string roleName = roleId switch
            {
                "1" => "Buyer",
                "2" => "Producer",
                "3" => "Admin",
                "4" => "MasterAdmin",
                _ => "Buyer"
            };

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("roleId", roleId),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("admin_authorized", roleId == "3" ? "true" : "false")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: _signingKey.CreateCredentials());

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
