using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Infrastructure.Helpers;
using Tequio.Infrastructure.Models;

namespace Tequio.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly TequioDbContext _dbContext;

        public AuthRepository(TequioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> CreateUserAsync(RegisterRequestDto request, string passwordHash)
        {
            try
            {
                return await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC USP_CreateUser @roleId, @email, @passwordHash, @firstName, @paternalLastName, @maternalLastName, @birthDate, @phoneNumber",
                    new SqlParameter("@roleId", request.RoleId),
                    new SqlParameter("@email", request.Email),
                    new SqlParameter("@passwordHash", passwordHash),
                    new SqlParameter("@firstName", request.FirstName),
                    new SqlParameter("@paternalLastName", request.PaternalLastName),
                    new SqlParameter("@maternalLastName", request.MaternalLastName ?? (object)DBNull.Value),
                    new SqlParameter("@birthDate", request.BirthDate),
                    new SqlParameter("@phoneNumber", request.PhoneNumber ?? (object)DBNull.Value)
                );
            }
            catch (SqlException ex)
            {
                throw SqlExceptionMapper.Map(ex);
            }
        }

        public async Task<(string? UserId, string? RoleId, string? PasswordHash)> GetUserAuthDataAsync(string email)
        {
            string? storedHash = null;
            string? userId = null;
            string? roleId = null;

            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "USP_GetUserByEmail";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@email", email));

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
            return (userId, roleId, storedHash);
        }
    }
}
