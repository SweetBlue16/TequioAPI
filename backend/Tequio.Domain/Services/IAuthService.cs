using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Infrastructure.Models;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for authentication and user management operations.
    /// </summary>
    public interface IAuthService
    {
        Task<int> RegisterUserAsync(RegisterRequestDto request);
        Task<string> AuthenticateUserAsync(LoginRequestDto request);
    }

    /// <summary>
    /// Implementation of authentication logic, hashing, and database communication.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly TequioDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthService(TequioDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<string> AuthenticateUserAsync(LoginRequestDto request)
        {
            string? storedHash = null;
            string? userId = null;
            string? roleId = null;

            using(var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "USP_GetUserByEmail";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@email", request.Email));

                await _dbContext.Database.OpenConnectionAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                        userId = reader.GetInt32(reader.GetOrdinal("Id")).ToString();
                        roleId = reader.GetInt32(reader.GetOrdinal("RoleId")).ToString();
                    }
                }
            }

            if (string.IsNullOrEmpty(storedHash))
            {
                throw new UnauthorizedAccessException(ErrorMessages.InvalidCredentials);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
            {
                throw new UnauthorizedAccessException(ErrorMessages.InvalidCredentials);
            }

            return GenerateJwtToken(userId!, roleId!);
        }

        public async Task<int> RegisterUserAsync(RegisterRequestDto request)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            try
            {
                var result = await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC USP_CreateUser @roleId, @email, @passwordHash, @firstName, @paternalLastName, @maternalLastName, @birthDate, @phoneNumber",
                    new SqlParameter("@roleId", request.RoleId),
                    new SqlParameter("@email", request.Email),
                    new SqlParameter("@passwordHash", passwordHash),
                    new SqlParameter("@firstName", request.FirstName),
                    new SqlParameter("@paternalLastName", request.PaternalLastName),
                    new SqlParameter("@maternalLastName", request.MaternalLastName) ?? (object)DBNull.Value,
                    new SqlParameter("@birthDate", request.BirthDate),
                    new SqlParameter("@phoneNumber", request.PhoneNumber) ?? (object)DBNull.Value
                );
                return result;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50001)
                {
                    throw new ArgumentException(ErrorMessages.AgeRestriction);
                }

                if (ex.Number == 50002)
                {
                    throw new ArgumentException(ErrorMessages.EmailAlreadyRegistered);
                }
                throw new InvalidOperationException(ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// Generates a JWT token for the authenticated user.
        /// </summary>
        private string GenerateJwtToken(string userId, string roleId)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Missing JWT Key configuration.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("roleId", roleId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
