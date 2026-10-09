using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Infrastructure.Helpers;
using Tequio.Infrastructure.Models;

namespace Tequio.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of user profile data access using Entity Framework and Stored Procedures.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly TequioDbContext _dbContext;

        public UserRepository(TequioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            try
            {
                await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = "USP_GetUserProfile";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@userId", userId));

                await _dbContext.Database.OpenConnectionAsync();
                await using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapToUserProfileDto(reader);
                }
                throw new KeyNotFoundException(ErrorMessages.RecordNotFound);
            }
            catch (SqlException ex)
            {
                throw SqlExceptionMapper.Map(ex);
            }
        }

        public async Task UpdateProfilePictureAsync(int userId, string profilePictureUrl)
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC USP_UpdateUserProfilePicture @userId, @profilePictureUrl",
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@profilePictureUrl", profilePictureUrl)
                );
            }
            catch (SqlException ex)
            {
                throw SqlExceptionMapper.Map(ex);
            }
        }

        public async Task UpdateUserProfileAsync(int userId, UpdateUserProfileDto request)
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC USP_UpdateUserProfile @userId, @firstName, @paternalLastName, @maternalLastName, @birthDate, @phoneNumber, @locality, @biography",
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@firstName", request.FirstName),
                    new SqlParameter("@paternalLastName", request.PaternalLastName),
                    new SqlParameter("@maternalLastName", request.MaternalLastName ?? (object)DBNull.Value),
                    new SqlParameter("@birthDate", request.BirthDate),
                    new SqlParameter("@phoneNumber", request.PhoneNumber ?? (object)DBNull.Value),
                    new SqlParameter("@locality", request.Locality ?? (object)DBNull.Value),
                    new SqlParameter("@biography", request.Biography ?? (object)DBNull.Value)
                );
            }
            catch (SqlException ex)
            {
                throw SqlExceptionMapper.Map(ex);
            }
        }

        private static UserProfileDto MapToUserProfileDto(DbDataReader reader)
        {
            return new UserProfileDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                PaternalLastName = reader.GetString(reader.GetOrdinal("PaternalLastName")),
                MaternalLastName = reader.IsDBNull(reader.GetOrdinal("MaternalLastName")) ? null : reader.GetString(reader.GetOrdinal("MaternalLastName")),
                BirthDate = reader.GetDateTime(reader.GetOrdinal("BirthDate")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                Locality = reader.IsDBNull(reader.GetOrdinal("Locality")) ? null : reader.GetString(reader.GetOrdinal("Locality")),
                Biography = reader.IsDBNull(reader.GetOrdinal("Biography")) ? null : reader.GetString(reader.GetOrdinal("Biography")),
                ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
                IsVerified = reader.GetBoolean(reader.GetOrdinal("IsVerified")),
                RegistrationDate = reader.GetDateTime(reader.GetOrdinal("RegistrationDate"))
            };
        }
    }
}
